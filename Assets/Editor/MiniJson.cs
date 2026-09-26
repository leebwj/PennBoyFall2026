using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// small reader for the design layout, which uses nested arrays and nulls that JsonUtility cannot read.
// objects become Dictionary<string, object>, arrays List<object>, numbers double
public static class MiniJson
{
    public static object Parse(string text)
    {
        int i = 0;
        object value = ReadValue(text, ref i);
        SkipSpace(text, ref i);
        if (i != text.Length) { throw new FormatException($"unexpected trailing data at {i}"); }
        return value;
    }

    private static object ReadValue(string s, ref int i)
    {
        SkipSpace(s, ref i);
        if (i >= s.Length) { throw new FormatException("unexpected end of json"); }
        char c = s[i];
        switch (c)
        {
            case '{': return ReadObject(s, ref i);
            case '[': return ReadArray(s, ref i);
            case '"': return ReadString(s, ref i);
            case 't': Expect(s, ref i, "true"); return true;
            case 'f': Expect(s, ref i, "false"); return false;
            case 'n': Expect(s, ref i, "null"); return null;
            default: return ReadNumber(s, ref i);
        }
    }

    private static Dictionary<string, object> ReadObject(string s, ref int i)
    {
        var result = new Dictionary<string, object>();
        i++;
        SkipSpace(s, ref i);
        if (s[i] == '}') { i++; return result; }
        while (true)
        {
            SkipSpace(s, ref i);
            string key = ReadString(s, ref i);
            SkipSpace(s, ref i);
            if (s[i] != ':') { throw new FormatException($"expected ':' at {i}"); }
            i++;
            result[key] = ReadValue(s, ref i);
            SkipSpace(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == '}') { i++; return result; }
            throw new FormatException($"expected ',' or '}}' at {i}");
        }
    }

    private static List<object> ReadArray(string s, ref int i)
    {
        var result = new List<object>();
        i++;
        SkipSpace(s, ref i);
        if (s[i] == ']') { i++; return result; }
        while (true)
        {
            result.Add(ReadValue(s, ref i));
            SkipSpace(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == ']') { i++; return result; }
            throw new FormatException($"expected ',' or ']' at {i}");
        }
    }

    private static string ReadString(string s, ref int i)
    {
        if (s[i] != '"') { throw new FormatException($"expected string at {i}"); }
        i++;
        var sb = new StringBuilder();
        while (s[i] != '"')
        {
            char c = s[i++];
            if (c != '\\') { sb.Append(c); continue; }
            char e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 't': sb.Append('\t'); break;
                case 'r': sb.Append('\r'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                default: sb.Append(e); break;
            }
        }
        i++;
        return sb.ToString();
    }

    private static double ReadNumber(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) { i++; }
        if (start == i) { throw new FormatException($"unexpected character '{s[i]}' at {i}"); }
        return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static void Expect(string s, ref int i, string word)
    {
        if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) { throw new FormatException($"expected {word} at {i}"); }
        i += word.Length;
    }

    private static void SkipSpace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) { i++; }
    }
}

// typed helpers over the parsed tree
public static class JsonTree
{
    public static Dictionary<string, object> Obj(object node, string key)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) ? v as Dictionary<string, object> : null;
    }

    public static List<object> List(object node, string key)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) ? v as List<object> : new List<object>();
    }

    public static bool Has(object node, string key)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) && v != null;
    }

    public static float Num(object node, string key, float fallback = 0f)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) && v is double d ? (float)d : fallback;
    }

    public static int Int(object node, string key, int fallback = 0)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) && v is double d ? (int)Math.Round(d) : fallback;
    }

    public static string Str(object node, string key)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) ? v as string : null;
    }

    public static bool Bool(object node, string key, bool fallback)
    {
        return ((Dictionary<string, object>)node).TryGetValue(key, out object v) && v is bool b ? b : fallback;
    }

    public static float[] Floats(object node, string key)
    {
        List<object> list = List(node, key);
        var result = new float[list.Count];
        for (int i = 0; i < list.Count; i++) { result[i] = (float)(double)list[i]; }
        return result;
    }

    public static float[] Floats(object array)
    {
        var list = (List<object>)array;
        var result = new float[list.Count];
        for (int i = 0; i < list.Count; i++) { result[i] = (float)(double)list[i]; }
        return result;
    }
}
