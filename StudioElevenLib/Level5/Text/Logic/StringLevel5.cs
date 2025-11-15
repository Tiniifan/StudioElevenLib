namespace StudioElevenLib.Level5.Text.Logic
{
    public class StringLevel5
    {
        public int TextNumber;
        public int VarianceKey;
        public string Text;

        public StringLevel5()
        {
        }

        public StringLevel5(int textNumber, string text)
        {
            TextNumber = textNumber;
            Text = text;
            VarianceKey = 0;
        }

        public StringLevel5(int textNumber, string text, int varianceKey)
        {
            TextNumber = textNumber;
            Text = text;
            VarianceKey = varianceKey;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}