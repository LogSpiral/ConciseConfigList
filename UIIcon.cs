using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.UI;

namespace ConciseConfigList;

public class UIIcon : UIElement
{
    public Asset<Texture2D> Icon { get; set; }

    public float Scale { get; set; }
}