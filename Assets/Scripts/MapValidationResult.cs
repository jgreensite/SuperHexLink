using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperHexLink.Validation
{
    /// <summary>
    /// Severity level of a validation issue.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational - not necessarily a problem.</summary>
        Info,
        /// <summary>Warning - may cause unexpected behavior.</summary>
        Warning,
        /// <summary>Error - will cause problems or crashes.</summary>
        Error
    }

    /// <summary>
    /// Category of validation issue for filtering and grouping.
    /// </summary>
    public enum ValidationCategory
    {
        /// <summary>Issues with hex grid structure (dimensions, null columns, etc.).</summary>
        GridStructure,
        /// <summary>Issues with hex state (missing type, invalid coordinates).</summary>
        HexState,
        /// <summary>Issues with land type configurations.</summary>
        LandConfig,
        /// <summary>Issues with number configurations.</summary>
        NumConfig,
        /// <summary>Issues with harbour placement or rotation.</summary>
        Harbour,
        /// <summary>Issues with hex references or relationships.</summary>
        Reference,
        /// <summary>Issues with GroupID matching.</summary>
        GroupID
    }

    /// <summary>
    /// Represents a single validation issue found in the map.
    /// </summary>
    [Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity Severity;
        public ValidationCategory Category;
        public string Message;
        public string Details;
        
        /// <summary>Column of the affected hex, or -1 if not hex-specific.</summary>
        public int Col = -1;
        /// <summary>Row of the affected hex, or -1 if not hex-specific.</summary>
        public int Row = -1;
        
        /// <summary>Name of the affected field or property.</summary>
        public string FieldName;
        
        /// <summary>Current value that caused the issue.</summary>
        public string CurrentValue;
        
        /// <summary>Suggested fix or expected value.</summary>
        public string SuggestedFix;

        public bool HasHexReference => Col >= 0 && Row >= 0;

        public string LocationString => HasHexReference ? $"[{Col},{Row}]" : "[N/A]";

        public override string ToString()
        {
            var prefix = Severity switch
            {
                ValidationSeverity.Error => "❌",
                ValidationSeverity.Warning => "⚠️",
                ValidationSeverity.Info => "ℹ️",
                _ => "•"
            };
            
            return HasHexReference 
                ? $"{prefix} [{Col},{Row}] {Message}" 
                : $"{prefix} {Message}";
        }

        public static ValidationIssue Error(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Category = category,
                Message = message,
                Col = col,
                Row = row
            };
        }

        public static ValidationIssue Warning(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = category,
                Message = message,
                Col = col,
                Row = row
            };
        }

        public static ValidationIssue Info(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            return new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = category,
                Message = message,
                Col = col,
                Row = row
            };
        }
    }

    /// <summary>
    /// Complete validation result containing all issues found.
    /// </summary>
    [Serializable]
    public class MapValidationResult
    {
        public List<ValidationIssue> Issues = new();
        public DateTime ValidatedAt = DateTime.Now;
        public string MapPath;
        
        // Summary counts
        public int ErrorCount => Issues.FindAll(i => i.Severity == ValidationSeverity.Error).Count;
        public int WarningCount => Issues.FindAll(i => i.Severity == ValidationSeverity.Warning).Count;
        public int InfoCount => Issues.FindAll(i => i.Severity == ValidationSeverity.Info).Count;
        public int TotalIssues => Issues.Count;
        
        public bool IsValid => ErrorCount == 0;
        public bool HasWarnings => WarningCount > 0;
        public bool HasIssues => TotalIssues > 0;

        public void AddIssue(ValidationIssue issue)
        {
            Issues.Add(issue);
        }

        public void AddError(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            Issues.Add(ValidationIssue.Error(category, message, col, row));
        }

        public void AddWarning(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            Issues.Add(ValidationIssue.Warning(category, message, col, row));
        }

        public void AddInfo(ValidationCategory category, string message, int col = -1, int row = -1)
        {
            Issues.Add(ValidationIssue.Info(category, message, col, row));
        }

        public List<ValidationIssue> GetIssuesByCategory(ValidationCategory category)
        {
            return Issues.FindAll(i => i.Category == category);
        }

        public List<ValidationIssue> GetIssuesBySeverity(ValidationSeverity severity)
        {
            return Issues.FindAll(i => i.Severity == severity);
        }

        public List<ValidationIssue> GetIssuesForHex(int col, int row)
        {
            return Issues.FindAll(i => i.Col == col && i.Row == row);
        }

        public string GetSummary()
        {
            if (!HasIssues)
            {
                return "✅ Map is valid - no issues found.";
            }

            return $"Found {TotalIssues} issue(s): {ErrorCount} error(s), {WarningCount} warning(s), {InfoCount} info";
        }
    }
}
