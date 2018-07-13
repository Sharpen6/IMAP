namespace IMAP.Forms.Draw
{
    internal class Box : Element
    {
        public Box(string name, bool heavy) : base(name + (heavy ? "*?" : "?"))
        {
        }
    }
}