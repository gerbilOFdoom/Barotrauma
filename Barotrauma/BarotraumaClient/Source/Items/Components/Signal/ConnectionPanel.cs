﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Linq;

namespace Barotrauma.Items.Components
{
    partial class ConnectionPanel : ItemComponent, IServerSerializable, IClientSerializable
    {
        public static Wire HighlightedWire;

        partial void InitProjSpecific(XElement element)
        {
            if (GuiFrame == null) return;
            new GUICustomComponent(new RectTransform(Vector2.One, GuiFrame.RectTransform), DrawConnections, null);
        }

        public override bool ShouldDrawHUD(Character character)
        {
            return character == Character.Controlled && character == user;
        }
        
        public override void AddToGUIUpdateList()
        {
            GuiFrame?.AddToGUIUpdateList();
        }

        public override void UpdateHUD(Character character, float deltaTime)
        {
            if (character != Character.Controlled || character != user) return;
            
            if (HighlightedWire != null)
            {
                HighlightedWire.Item.IsHighlighted = true;
                if (HighlightedWire.Connections[0] != null && HighlightedWire.Connections[0].Item != null) HighlightedWire.Connections[0].Item.IsHighlighted = true;
                if (HighlightedWire.Connections[1] != null && HighlightedWire.Connections[1].Item != null) HighlightedWire.Connections[1].Item.IsHighlighted = true;
            }
        }

        private void DrawConnections(SpriteBatch spriteBatch, GUICustomComponent container)
        {
            if (user != Character.Controlled || user == null) return;

            HighlightedWire = null;
            Connection.DrawConnections(spriteBatch, this, user);

            foreach (UISprite sprite in GUI.Style.GetComponentStyle("ConnectionPanelFront").Sprites[GUIComponent.ComponentState.None])
            {
                sprite.Draw(spriteBatch, GuiFrame.Rect, Color.White, SpriteEffects.None);
            }
        }
    }
}
