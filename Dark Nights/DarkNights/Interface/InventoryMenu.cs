using Nebula.Main;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Nebula.Input;

namespace DarkNights.Interface
{
    public class InventoryMenu : MenuBase
    {
        //
        private EntityInventory m_selected;
        // Hands
        private InventorySlot leftHandSlot;
        private InventorySlot rightHandSlot;
        // Slots
        private InventorySlot backpackSlot;


        public InventoryMenu()
        {
            var background = new ExpandableTexture(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(64, 0, 64, 64), 0, 0, 0, 0);
            var outlineL = new ExpandableTexture(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(64, 64, 64, 64), 2, 0, 2, 2);
            var outlineR = new ExpandableTexture(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(128, 64, 64, 64), 0, 2, 2, 2);
            var outlineB = new ExpandableTexture(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(192, 64, 64, 64), 2, 2, 2, 2);

            leftHandSlot = new InventorySlot(new Rectangle(10, Graphics.RENDER_HEIGHT - 74, 64, 64), background, outlineL);
            rightHandSlot = new InventorySlot(new Rectangle(74, Graphics.RENDER_HEIGHT - 74, 64, 64), background, outlineR);
            backpackSlot = new InventorySlot(new Rectangle(158, Graphics.RENDER_HEIGHT - 74, 64, 64), background, outlineB);
        }

        public void Set(EntityInventory inventory)
        {
            if (inventory == m_selected) return;

            if (m_selected != null)
            {
                m_selected.cbOnInventoryChange -= SetInventoryUpdate;
            }

            m_selected = inventory;
            if (m_selected == null) return;
            m_selected.cbOnInventoryChange += SetInventoryUpdate;
            SetInventoryUpdate();

        }

        public override void Draw()
        {
            if (IsOpen)
            {
                leftHandSlot.Draw();
                rightHandSlot.Draw();
                backpackSlot.Draw();
            }
        }

        public override void Tick()
        {
            if (IsOpen)
            {
                leftHandSlot.Tick();
                rightHandSlot.Tick();
                backpackSlot.Tick();
            }
        }

        public override bool OpenMenu()
        {
            leftHandSlot.Enable();
            rightHandSlot.Enable();
            backpackSlot.Enable();
            return base.OpenMenu();
        }

        public override bool CloseMenu()
        {
            leftHandSlot.Disable();
            rightHandSlot.Disable();
            backpackSlot.Disable();
            return base.CloseMenu();
        }

        public void DropItem(InventorySlot slot, Vector2 pos)
        {
            Coordinates tile = Camera.ScreenToWorld(new Vector2(pos.X, pos.Y));
            InterfaceController.log.Info($"Drop Item @ {tile}");
            if (slot == leftHandSlot && m_selected.leftHand != null)
            {
                m_selected.AttemptDropItem(m_selected.leftHand, tile);
            }
            if (slot == rightHandSlot && m_selected.rightHand != null)
            {
                m_selected.AttemptDropItem(m_selected.rightHand, tile);
            }
            if (slot == backpackSlot && m_selected.backpack != null)
            {
                m_selected.AttemptDropItem(m_selected.backpack, tile);
            }
        }

        private void SetInventoryUpdate()
        {
            if (m_selected == null) return;
            leftHandSlot.Set(m_selected.leftHand);
            rightHandSlot.Set(m_selected.rightHand);
            backpackSlot.Set(m_selected.backpack);
        }
    }

    public class InventorySlot
    {
        public Rectangle Bounds { get; private set; }

        public IPointerEventListener Parent => null;
        public IPointerEventListener[] Children => null;

        private InterfaceButton button;
        private Sprite2D itemSprite;
        private ExpandableTexture background;
        private ExpandableTexture outline;

        private Rectangle itemRect;

        public InventorySlot(Rectangle bounds, ExpandableTexture background, ExpandableTexture outline)
        {
            Bounds = bounds;
            this.background = background;
            this.outline = outline;

            itemRect = new Rectangle(Bounds.X + 4, Bounds.Y + 4, Bounds.Width - 8, Bounds.Height - 8);
            button = new InterfaceButton(Bounds,
                new Color(255, 255, 255, 50),
                new Color(255, 255, 255, 40),
                new Color(255, 255, 255, 30),
                new Color(0, 0, 0, 0),
                new Color(50, 50, 50, 25));
            button.Interactable = true;
            button.OnEndDrag += EndDrag;
        }

        public void Set(IItem item)
        {
            if (item == null)
            {
                itemSprite = null;
                return;
            }
            itemSprite = item.Sprite;
        }

        public void Tick()
        {
            button.Tick();
        }

        public void Draw()
        {
            UserInterface.Get.DrawSlicedSprite(background, Bounds, Color.White);
            UserInterface.Get.DrawSlicedSprite(outline, Bounds, Color.White);

            if (itemSprite != null)
            {
                UserInterface.Get.DrawUI(itemSprite.Texture, itemRect, itemSprite.SourceRect, Color.White, 0, Vector2.Zero, false, false, false);
            }

            button.Draw();
        }

        public void Enable()
        {
            button.SetActive(true);
        }

        public void Disable()
        {
            button.SetActive(false);
        }

        public void EndDrag(InterfaceButton button, MouseButtonActionState data)
        {
            InterfaceController.Get.InventoryMenu.DropItem(this, data.mousePosition.ToVector2());
        }
    }
}
