/*
 * Original code by hubeza on Stackoverflow: http://stackoverflow.com/a/19353995/52360
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Shell.Common.IO;
using Shell.Common.Util;

namespace Shell.Common.IO
{
    public static class TableParserExtensions
    {
        public static object[] ToStringTable<T> (this IEnumerable<T> values, string[] columnHeaders, params Func<T, object>[] valueSelectors)
        {
            return ToStringTable (values: values, highlightColor: v => LogColor.Reset, columnHeaders: columnHeaders, valueSelectors: valueSelectors);
        }

        public static object[] ToStringTable<T> (this IEnumerable<T> values, Func<T, LogColor> highlightColor, string[] columnHeaders, params Func<T, object>[] valueSelectors)
        {
            T[] _values = values.ToArray ();

            Debug.Assert (columnHeaders.Length == valueSelectors.Length);

            string[,] arrValues = new string[_values.Length + 1, valueSelectors.Length];
            LogColor[] highlightColors = new LogColor[_values.Length + 1];

            // Fill headers
            for (int colIndex = 0; colIndex < arrValues.GetLength (1); colIndex++) {
                arrValues [0, colIndex] = columnHeaders [colIndex];
            }
            highlightColors [0] = LogColor.Reset;

            // Fill table rows
            for (int rowIndex = 1; rowIndex < arrValues.GetLength (0); rowIndex++) {
                for (int colIndex = 0; colIndex < arrValues.GetLength (1); colIndex++) {
                    object value = valueSelectors [colIndex].Invoke (_values [rowIndex - 1]);

                    arrValues [rowIndex, colIndex] = value != null ? value.ToString () : "null";
                }
                highlightColors [rowIndex] = highlightColor (_values [rowIndex - 1]);
            }

            return ToStringTable (arrValues: arrValues, highlightColors: highlightColors);
        }

        private static object[] ToStringTable (this string[,] arrValues, LogColor[] highlightColors)
        {
            int[] maxColumnsWidth = GetMaxColumnsWidth (arrValues);
            string headerSplitter = new string ('-', maxColumnsWidth.Sum (i => i + 3) - 1);

            List<object> objs = new List<object> ();
            objs.Add (ShellCharacters.Newline);
            for (int rowIndex = 0; rowIndex < arrValues.GetLength (0); rowIndex++) {

                LogColor highlightColor = highlightColors [rowIndex];

                for (int colIndex = 0; colIndex < arrValues.GetLength (1); colIndex++) {
                    // Print cell
                    string cell = arrValues [rowIndex, colIndex];
                    cell = cell.PadRight (maxColumnsWidth [colIndex]);
                    objs.AddRange (LogColor.DarkBlue, " | ", LogColor.Reset);
                    objs.AddRange (highlightColor, cell, LogColor.Reset);
                }

                // Print end of line
                objs.AddRange (LogColor.DarkBlue, " | ", LogColor.Reset);
                objs.Add (ShellCharacters.Newline);

                // Print splitter
                if (rowIndex == 0) {
                    objs.AddRange (LogColor.DarkBlue, string.Format (" |{0}| ", headerSplitter), LogColor.Reset);
                    objs.Add (ShellCharacters.Newline);
                }
            }

            return objs.ToArray ();
        }

        private static int[] GetMaxColumnsWidth (string[,] arrValues)
        {
            var maxColumnsWidth = new int[arrValues.GetLength (1)];
            for (int colIndex = 0; colIndex < arrValues.GetLength (1); colIndex++) {
                for (int rowIndex = 0; rowIndex < arrValues.GetLength (0); rowIndex++) {
                    int newLength = arrValues [rowIndex, colIndex].Length;
                    int oldLength = maxColumnsWidth [colIndex];

                    if (newLength > oldLength) {
                        maxColumnsWidth [colIndex] = newLength;
                    }
                }
            }

            return maxColumnsWidth;
        }

        public static object[] ToStringTable<T> (this IEnumerable<T> values, params Expression<Func<T, object>>[] valueSelectors)
        {
            var headers = valueSelectors.Select (func => GetProperty (func).Name).ToArray ();
            var selectors = valueSelectors.Select (exp => exp.Compile ()).ToArray ();
            return ToStringTable (values, headers, selectors);
        }

        private static PropertyInfo GetProperty<T> (Expression<Func<T, object>> expresstion)
        {
            if (expresstion.Body is UnaryExpression) {
                if ((expresstion.Body as UnaryExpression).Operand is MemberExpression) {
                    return ((expresstion.Body as UnaryExpression).Operand as MemberExpression).Member as PropertyInfo;
                }
            }

            if ((expresstion.Body is MemberExpression)) {
                return (expresstion.Body as MemberExpression).Member as PropertyInfo;
            }
            return null;
        }
    }
}

