namespace Panther.CodeAnalysis.Text
{
    public sealed class NoSourceFile : SourceFile
    {
        public override int LineToOffset(int index) => -1;
        public override int GetLineIndex(int position) => -1;
        public override string ToString() => "<no source file>";

        internal NoSourceFile()
            : base("", "")
        {
        }
    }
}