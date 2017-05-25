using System.Collections.Generic;
using System.Linq;

namespace CoPilot.ORM.Scripting
{

    public interface IBlockItem
    {
        int Level { get; set; }
    }

    public class TextLine : IBlockItem
    {
        public TextLine(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
        public int Level { get; set; }

        public override string ToString()
        {
            return ScriptBlock.GetIndent(Level) + Text;
        }
    }

    public class ScriptBlock : IBlockItem
    {
        public ScriptBlock()
        {
            Items = new List<IBlockItem>();
        }

        public ScriptBlock(params string[] text)
        {
            Items = new List<IBlockItem>();
            Add(text);
        }

        private List<IBlockItem> Items { get; }
        private int _level;
        public int Level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value + 1;

                foreach (var item in Items)
                {
                    item.Level = _level;
                }
            }
        }

        public int ItemCount => Items.Count;

        internal static string GetIndent(int indent)
        {
            const string t = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";
            return t.Substring(0, indent);
        }

        public void Append(ScriptBlock block)
        {
            foreach (var blockItem in block.Items)
            {
                Add(blockItem);
            }
        }

        public void Add(IBlockItem item)
        {
            item.Level = Level;

            Items.Add(item);
        }

        public void Add()
        {
            Items.Add(new TextLine(""));
        }

        public void Add(params string[] linesOfText)
        {
            var items = linesOfText.Select(r => new TextLine(r) { Level = Level });

            Items.AddRange(items);
        }

        public void AddAsNewBlock(params string[] linesOfText)
        {
            var block = new ScriptBlock { Level = Level };
            var items = linesOfText.Select(r => new TextLine(r) { Level = block.Level });

            block.Items.AddRange(items);
            Items.Add(block);
        }

        public void WrapInside(string top, string bottom, bool indent = true)
        {

            var block = new ScriptBlock();
            block.Append(this);
            if (indent)
            {
                block.Level = Level;
            }
            else
            {
                block.Level = Level - 1;
            }

            Items.Clear();
            Add(top);
            Items.Add(block);
            Add(bottom);
          
        }

        public void AddMultiLineText(string linesOfText, bool addAsNewBlock = true)
        {
            var items = linesOfText.Split('\n');
            if (addAsNewBlock)
            {
                var block = new ScriptBlock { Level = Level };
                block.Add(items);

                Items.Add(block);
            }
            else
            {
                Add(items);
            }
        }

        public override string ToString()
        {
            return string.Join("\n", Items);
        }
    }

}
