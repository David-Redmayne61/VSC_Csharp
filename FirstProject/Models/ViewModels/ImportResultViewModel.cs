using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstProject.Models.ViewModels
{
    public class ImportResultViewModel
    {
        public int SuccessCount { get; set; }
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
        public List<DuplicateEntry> Duplicates { get; set; } = new List<DuplicateEntry>();

        public IEnumerable<IGrouping<char, DuplicateEntry>> DuplicatesByLetter =>
            Duplicates.OrderBy(d => d.Username)
                     .GroupBy(d => char.ToUpper(d.Username[0]));
    }

    public class ImportError
    {
        public string Line { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public class DuplicateEntry
    {
        public string Username { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public DateTime AttemptedImport { get; set; }
    }
}