using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Runtime;

internal sealed class GameFormulaEvaluator
{
    private readonly GameProjectData _project;
    private readonly SaveGame _save;
    private readonly Func<Dictionary<string, int>> _effectiveStatsProvider;
    private readonly IReadOnlyDictionary<string, int>? _actorStats;
    private readonly IReadOnlyDictionary<string, int>? _targetStats;
    private readonly string _text;
    private int _position;

    public GameFormulaEvaluator(GameProjectData project, SaveGame save, string expression, Func<Dictionary<string, int>> effectiveStatsProvider, IReadOnlyDictionary<string, int>? actorStats = null, IReadOnlyDictionary<string, int>? targetStats = null)
    {
        _project = project;
        _save = save;
        _text = expression ?? string.Empty;
        _effectiveStatsProvider = effectiveStatsProvider;
        _actorStats = actorStats;
        _targetStats = targetStats;
    }

    public int Evaluate()
    {
        var result = TryEvaluate();
        return result.Success ? result.Value : 0;
    }

    public GameFormulaEvaluationResult TryEvaluate()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            return new GameFormulaEvaluationResult { Success = false, Message = "Формула пустая." };
        }

        try
        {
            var value = ParseExpression();
            SkipWhiteSpace();
            if (_position < _text.Length)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Лишний токен в формуле: " + Peek() };
            }

            return new GameFormulaEvaluationResult { Success = true, Value = value, Message = "OK" };
        }
        catch (Exception ex)
        {
            return new GameFormulaEvaluationResult { Success = false, Message = ex.Message };
        }
    }

    private int ParseExpression()
    {
        var value = ParseTerm();
        while (true)
        {
            SkipWhiteSpace();
            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                return value;
            }
        }
    }

    private int ParseTerm()
    {
        var value = ParseFactor();
        while (true)
        {
            SkipWhiteSpace();
            if (Match('*'))
            {
                value *= ParseFactor();
            }
            else if (Match('/'))
            {
                var divisor = ParseFactor();
                if (divisor == 0)
                {
                    throw new InvalidOperationException("Деление на ноль в формуле.");
                }

                value /= divisor;
            }
            else if (Match('%'))
            {
                var divisor = ParseFactor();
                if (divisor == 0)
                {
                    throw new InvalidOperationException("Деление по модулю на ноль в формуле.");
                }

                value %= divisor;
            }
            else
            {
                return value;
            }
        }
    }

    private int ParseFactor()
    {
        SkipWhiteSpace();
        if (Match('-'))
        {
            return -ParseFactor();
        }

        if (Match('('))
        {
            var value = ParseExpression();
            Expect(')');
            return value;
        }

        if (char.IsDigit(Peek()))
        {
            return ParseNumber();
        }

        if (IsIdentifierStart(Peek()))
        {
            var name = ParseIdentifier();
            SkipWhiteSpace();
            return Match('(') ? ParseFunction(name) : ResolveVariable(name);
        }

        throw new InvalidOperationException("Unexpected formula token.");
    }

    private int ParseFunction(string name)
    {
        var args = new List<int>();
        SkipWhiteSpace();
        if (!Match(')'))
        {
            while (true)
            {
                args.Add(ParseExpression());
                SkipWhiteSpace();
                if (Match(')'))
                {
                    break;
                }

                Expect(',');
            }
        }

        return name.ToLowerInvariant() switch
        {
            "min" when args.Count == 2 => Math.Min(args[0], args[1]),
            "max" when args.Count == 2 => Math.Max(args[0], args[1]),
            "clamp" when args.Count == 3 => Math.Clamp(args[0], args[1], args[2]),
            "abs" when args.Count == 1 => Math.Abs(args[0]),
            "percent" when args.Count == 2 => args[0] * args[1] / 100,
            "random" when args.Count == 2 => RandomInclusive(args[0], args[1]),
            "dice" when args.Count == 2 => RollDice(args[0], args[1]),
            _ => throw new InvalidOperationException("Неизвестная функция или неверное число аргументов: " + name)
        };
    }

    private int ResolveVariable(string name)
    {
        if (string.Equals(name, "turn", StringComparison.OrdinalIgnoreCase))
        {
            return _save.TurnNumber;
        }

        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Неизвестный идентификатор формулы: " + name);
        }

        var scope = parts[0].ToLowerInvariant();
        var id = parts[1];
        return scope switch
        {
            "player" when string.Equals(id, "level", StringComparison.OrdinalIgnoreCase) => _save.PlayerLevel,
            "player" when string.Equals(id, "experience", StringComparison.OrdinalIgnoreCase) => _save.PlayerExperience,
            "actor" => ResolveCombatStat("actor", _actorStats, id),
            "target" => ResolveCombatStat("target", _targetStats, id),
            "stat" => ResolveStat(id),
            "effectivestat" => ResolveEffectiveStat(id),
            "currency" => ResolveCurrency(id),
            "variable" => ResolveVariableValue(id),
            "relationship" => ResolveRelationship(id),
            "world" when string.Equals(id, "day", StringComparison.OrdinalIgnoreCase) => Math.Max(1, _save.WorldState.DayNumber),
            "world" when string.Equals(id, "turn", StringComparison.OrdinalIgnoreCase) => _save.TurnNumber,
            "world" when string.Equals(id, "timeSegmentOrder", StringComparison.OrdinalIgnoreCase) => ResolveTimeSegmentOrder(),
            "item" when parts.Length >= 3 && string.Equals(parts[2], "quantity", StringComparison.OrdinalIgnoreCase) => GetItemQuantity(id),
            "skill" when parts.Length >= 3 && string.Equals(parts[2], "level", StringComparison.OrdinalIgnoreCase) => GetSkillLevel(id),
            "skill" when parts.Length >= 3 && string.Equals(parts[2], "experience", StringComparison.OrdinalIgnoreCase) => GetSkillExperience(id),
            "status" when parts.Length >= 3 && string.Equals(parts[2], "stacks", StringComparison.OrdinalIgnoreCase) => GetStatusStacks(id),
            _ => throw new InvalidOperationException("Неизвестный идентификатор формулы: " + name)
        };
    }

    private static int ResolveCombatStat(string scope, IReadOnlyDictionary<string, int>? stats, string id)
    {
        if (stats != null && stats.TryGetValue(id, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("РќРµРёР·РІРµСЃС‚РЅС‹Р№ " + scope + " stat: " + id);
    }

    private int ResolveTimeSegmentOrder()
    {
        var segment = _project.WorldState.Time.Segments.FirstOrDefault(x => string.Equals(x.Id, _save.WorldState.TimeSegmentId, StringComparison.OrdinalIgnoreCase));
        return segment?.Order ?? 0;
    }

    private int ResolveStat(string id)
    {
        if (_save.PlayerStats.TryGetValue(id, out var value))
        {
            return value;
        }

        var definition = _project.Stats.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (definition != null)
        {
            return definition.InitialValue;
        }

        throw new InvalidOperationException("Неизвестный stat: " + id);
    }

    private int ResolveEffectiveStat(string id)
    {
        var stats = _effectiveStatsProvider();
        if (stats.TryGetValue(id, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("Неизвестный effectiveStat: " + id);
    }

    private int ResolveCurrency(string id)
    {
        if (_save.Currencies.TryGetValue(id, out var value))
        {
            return value;
        }

        var definition = _project.Currencies.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (definition != null)
        {
            return definition.InitialAmount;
        }

        throw new InvalidOperationException("Неизвестная currency: " + id);
    }

    private int ResolveVariableValue(string id)
    {
        if (_save.Variables.TryGetValue(id, out var value))
        {
            return value;
        }

        var definition = _project.Variables.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (definition != null)
        {
            return definition.InitialValue;
        }

        throw new InvalidOperationException("Неизвестная variable: " + id);
    }

    private int ResolveRelationship(string id)
    {
        if (_save.Relationships.TryGetValue(id, out var value))
        {
            return value;
        }

        var definition = _project.Relationships.FirstOrDefault(x => string.Equals(x.CharacterId, id, StringComparison.OrdinalIgnoreCase));
        if (definition != null)
        {
            return definition.InitialValue;
        }

        throw new InvalidOperationException("Неизвестное relationship: " + id);
    }

    private int GetItemQuantity(string itemId)
    {
        if (_project.Items.All(x => !string.Equals(x.Id, itemId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Неизвестный item: " + itemId);
        }

        if (_save.InventoryEntries.Count > 0)
        {
            return _save.InventoryEntries
                .Where(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Quantity);
        }

        return _save.Inventory.GetValueOrDefault(itemId);
    }

    private int GetSkillLevel(string skillId)
    {
        if (_project.Skills.All(x => !string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Неизвестный skill: " + skillId);
        }

        return _save.KnownSkills
            .FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
            ?.Level ?? 0;
    }

    private int GetSkillExperience(string skillId)
    {
        if (_project.Skills.All(x => !string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("??????????? skill: " + skillId);
        }

        return _save.KnownSkills
            .FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
            ?.Experience ?? 0;
    }

    private int GetStatusStacks(string statusEffectId)
    {
        if (_project.StatusEffects.All(x => !string.Equals(x.Id, statusEffectId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Неизвестный status: " + statusEffectId);
        }

        return _save.ActiveStatusEffects
            .Where(x => string.Equals(x.StatusEffectId, statusEffectId, StringComparison.OrdinalIgnoreCase))
            .Sum(x => Math.Max(0, x.Stacks));
    }

    private static int RandomInclusive(int min, int max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return Random.Shared.Next(min, max + 1);
    }

    private static int RollDice(int count, int sides)
    {
        count = Math.Clamp(count, 1, 20);
        sides = Math.Clamp(sides, 1, 100);
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += Random.Shared.Next(1, sides + 1);
        }

        return total;
    }

    private int ParseNumber()
    {
        var start = _position;
        while (char.IsDigit(Peek()))
        {
            _position++;
        }

        return int.TryParse(_text[start.._position], out var value) ? value : 0;
    }

    private string ParseIdentifier()
    {
        var start = _position;
        while (IsIdentifierPart(Peek()))
        {
            _position++;
        }

        return _text[start.._position];
    }

    private void Expect(char expected)
    {
        SkipWhiteSpace();
        if (!Match(expected))
        {
            throw new InvalidOperationException("Formula token expected.");
        }
    }

    private bool Match(char ch)
    {
        if (Peek() != ch)
        {
            return false;
        }

        _position++;
        return true;
    }

    private char Peek()
    {
        return _position < _text.Length ? _text[_position] : '\0';
    }

    private void SkipWhiteSpace()
    {
        while (char.IsWhiteSpace(Peek()))
        {
            _position++;
        }
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.';
    }
}
