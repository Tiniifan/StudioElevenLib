namespace StudioElevenLib.Level5.Text.Logic
{
    public class StringLevel5
    {
        public int Variance;

        public string Text;

        public StringLevel5()
        {

        }

        public StringLevel5(int variance, string text)
        {
            Variance = variance;
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
