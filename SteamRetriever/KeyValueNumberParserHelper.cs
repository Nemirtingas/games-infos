using SteamKit2;
using SteamRetriever.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SteamRetriever;

public static class KeyValueNumberParserHelper
{
    public const string MAX_INT = "MAX_INT";
    public const string MIN_INT = "MIN_INT";

    public const string MAX_FLOAT = "MAX_FLOAT";
    public const string MIN_FLOAT = "MIN_FLOAT";

    public const string INFINITY = "INFINITY";
    public const string MINUS_INFINITY = "-INFINITY";

    public static bool KeyValueValueToBoolean(this KeyValue kv)
    {
        var stringV = kv.AsString()?.ToLowerInvariant();
        if (stringV == null)
            return false;

        if (stringV == "true")
            return true;

        if (stringV == "false")
            return false;

        if (long.TryParse(stringV, out var v))
            return v != 0;

        return false;
    }

    static bool TryParseStatValue(string s, out decimal? value, out string numberRepresentation)
    {
        value = null;
        numberRepresentation = null;

        bool negativeHex = s.StartsWith("-0x", StringComparison.OrdinalIgnoreCase);
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || negativeHex)
        {
            string hexPart = negativeHex ? s[3..] : s[2..];
            try
            {
                var bytes = Convert.FromHexString(hexPart);

                var hexValue = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);

                if (negativeHex)
                    hexValue = -hexValue;

                if (hexValue < (BigInteger)decimal.MinValue)
                    numberRepresentation = MIN_INT;
                else if (hexValue > (BigInteger)decimal.MaxValue)
                    numberRepresentation = MAX_INT;
                else
                    value = (decimal)hexValue;

                return true;
            }
            catch { }
        }

        if (s.EndsWith("f", StringComparison.OrdinalIgnoreCase) || s.EndsWith("d", StringComparison.OrdinalIgnoreCase))
            s = s[..^1];

        if (s.Count(c => c == ',') > 1)
            s = s.Replace(",", "");
        else if (s.Contains(',') && !s.Contains('.'))
            s = s.Replace(',', '.');

        if (decimal.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dValue))
        {
            value = dValue;
            return true;
        }

        if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dblValue))
        {
            if (double.IsNaN(dblValue))
                return false;

            if (double.IsPositiveInfinity(dblValue))
                numberRepresentation = INFINITY;
            else if (double.IsNegativeInfinity(dblValue))
                numberRepresentation = MINUS_INFINITY;
            else if (dblValue < (double)decimal.MinValue)
                numberRepresentation = MIN_FLOAT;
            else if (dblValue > (double)decimal.MaxValue)
                numberRepresentation = MAX_FLOAT;
            else
                value = (decimal)dblValue;

            return true;
        }

        return false;
    }

    static string NormalizeKeyValueString(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        // Convert FullWidth numbers to regular ones
        // 3256310 ０, 971620 ０,１, ...
        s = s.Normalize(NormalizationForm.FormKC);
        s = s.ToLowerInvariant();
        s = s.Replace("\\t", null)
            .Replace("\\r", null)
            .Replace("\\n", null);
        s = new string(s.Where(c => c != '\'' && !char.IsWhiteSpace(c)).ToArray());

        return s;
    }

    public static string StringNumberRepresentationToMatchingStatTypeNumberRepresentation(string numberRepresentation, SchemaStatType statType)
    {
        switch (statType)
        {
            case SchemaStatType.Int:
                switch (numberRepresentation)
                {
                    case MIN_INT: case MIN_FLOAT: 
                    case MINUS_INFINITY:
                        return MIN_INT;

                    case MAX_INT: case MAX_FLOAT:
                    case INFINITY:
                        return MAX_INT;
                }
                break;

            case SchemaStatType.Float: 
            case SchemaStatType.AvgRate:
                return numberRepresentation;
        }

        return null;
    }

    static bool IsWellKnownStringNumberRepresentation(string s, out object value)
    {
        // We treat inverse of min/max as their opposite counterpart as its more an idea of the opposite of min/max instead of their real bit values.
        // Example, inverse of chocolate is vanilla.
        switch (s)
        {
            case "-int_min": case "-min_int":
            case "int_max": case "max_int": value = MAX_INT; return true;

            case "-int_max": case "-max_int":
            case "int_min": case "min_int": value = MIN_INT; return true;

            case "-flt_min": case "-min_flt": case "-min_float": case "-float_min":
            case "flt_max" : case "max_flt" : case "max_float" : case "float_max" : value = MAX_FLOAT; return true;

            case "-flt_max": case "-max_flt": case "-max_float": case "-float_max":
            case "flt_min" : case "min_flt" : case "min_float" : case "float_min" : value = MIN_FLOAT; return true;

            case "inf": value = INFINITY; return true;

            case "-inf": value = MINUS_INFINITY; return true;
        }

        value = null;
        return false;
    }

    public static bool StringToClosestNumberRepresentation(this KeyValue kv, out object value)
    {
        value = null;

        if (kv == KeyValue.Invalid || kv == null)
            return true;

        var stringV = NormalizeKeyValueString(kv.AsString());
        if (string.IsNullOrWhiteSpace(stringV))
            return true;

        // Handle special case where "o" is used instead of "0" for zero value, we can find this in some achievements progression values for example.
        // 1951780, 1520330, 412050, ...

        if (IsWellKnownStringNumberRepresentation(stringV, out value))
            return true;

        stringV = stringV.Replace('o', '0');

        if (TryParseStatValue(stringV, out var dValue, out var sValue))
        {
            if (dValue != null)
                value = dValue;
            else
                value = sValue;
                
            return true;
        }

        return false;
    }

    public static object CastNumberRepresentationToStatType(SchemaStatType statType, object value)
    {
        if (value == null)
            return null;

        if (value is string s)
            return StringNumberRepresentationToMatchingStatTypeNumberRepresentation(s, statType);

        var d = (decimal)value;
        switch (statType)
        {
            case SchemaStatType.Int:
                if (d >= int.MaxValue)
                    return KeyValueNumberParserHelper.MAX_INT;
                else if (d <= int.MinValue)
                    return KeyValueNumberParserHelper.MIN_INT;
                else
                    return (int)d;

            case SchemaStatType.AvgRate:
            case SchemaStatType.Float:
                return d;
        }

        return null;
    }
}
