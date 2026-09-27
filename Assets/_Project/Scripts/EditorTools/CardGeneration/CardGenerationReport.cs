using System.Collections.Generic;
using System.Text;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>What one generator run did: asset counts, notes and errors.</summary>
    public sealed class CardGenerationReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _notes = new List<string>();

        public int Created { get; private set; }

        public int Updated { get; private set; }

        public int Unchanged { get; private set; }

        public int Deleted { get; private set; }

        public IReadOnlyList<string> Errors => _errors;

        public IReadOnlyList<string> Notes => _notes;

        public bool Succeeded => _errors.Count == 0;

        /// <summary>Records the outcome of one "ensure this asset" step.</summary>
        public void Count(AssetChange change)
        {
            switch (change)
            {
                case AssetChange.Created:
                    Created++;
                    break;
                case AssetChange.Updated:
                    Updated++;
                    break;
                default:
                    Unchanged++;
                    break;
            }
        }

        public void CountDeleted() => Deleted++;

        public void AddError(string message) => _errors.Add(message);

        public void AddNote(string message) => _notes.Add(message);

        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append(Succeeded ? "Generated" : "Generation failed")
                .Append($": {Created} created, {Updated} updated, {Unchanged} unchanged, {Deleted} deleted.");
            foreach (string error in _errors) text.Append("\nError: ").Append(error);
            foreach (string note in _notes) text.Append("\n").Append(note);
            return text.ToString();
        }
    }
}
