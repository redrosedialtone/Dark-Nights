using DarkNights.Interface;
using DarkNights.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using Nebula.Runtime;
using Nebula.Systems;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Runtime.DrawUtils;

namespace DarkNights
{
    public class PlayerController : Manager, IPlayerInputCtxt
    {
        #region Static
        private static PlayerController instance;
        public static PlayerController Get => instance;

        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("PLAYER");

        public List<string> Logs { get; set; } = new List<string>();

        #endregion

        public List<Character> Characters;
        public List<Wall> Walls = new List<Wall>();
        public Action<Wall> OnWallBuilt;

        public Character selectedCharacter;
        private CharacterGizmo characterGizmo;
        private EntityGizmo entityGizmo;
        private SelectionOverlay selectionOverlay;

        public override void Init()
        {
            log.Info("> ...");
            instance = this;

            CharacterControlCtxt ctxt = new CharacterControlCtxt();
            Input.Get.AddContext(ctxt);
            Input.Get.EnableContext(ctxt.Name);
            ctxt.OnClick += this.OnClick;

            ApplicationController.Get.Initiate(this);
        }

        public override void OnInitialized()
        {
            base.OnInitialized();

            characterGizmo = new CharacterGizmo();
            characterGizmo.Enabled = true;
            characterGizmo.DrawCharacters = true;
            //characterGizmo.DrawCharacterPaths = true;

            entityGizmo = new EntityGizmo();
            entityGizmo.Enabled = true;
            entityGizmo.DrawWalls = true;

            Character Jerry = new Character("Jerry", new Coordinates(10, 10));
            Character Jones = new Character("Jones", new Coordinates(15, 10));
            Characters = new List<Character>() { Jerry, Jones };

            TaskSystem.Worker(Jerry);
            TaskSystem.Worker(Jones);

            EntityController.Get.PlaceEntityInWorld(new Hammer(new Coordinates(12, 12)));
            EntityController.Get.PlaceEntityInWorld(new Axe(new Coordinates(14, 12)));

            selectionOverlay = new SelectionOverlay();

        }

        public override void Tick()
        {
            base.Tick();
            foreach (var character in Characters)
            {
                character.Tick();
            }
        }

        public override void Draw()
        {
            base.Draw();
            foreach (var character in Characters)
            {
                SpriteBatchRenderer.Get.DrawSprite(character.Sprite, character.Position, character.Rotation);
            }
            selectionOverlay.Draw();
        }

        public void OnMovementAxis(Vector2 movementAxis)
        {
            
        }

        public void OnRotate(float rotation)
        {
            
        }

        public void OnZoom(float zoomDelta)
        {
            
        }

        public void OnLock(bool locked)
        {
            
        }

        private Coordinates _lastWallPos;
        private Coordinates _lastMovePos;
        public void OnClick(MouseButtonActionState Data)
        {
            if (Data.ID == "InputID.LeftMouseButton")
            {
                Coordinates mousePos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));

                // Select a character under the mouse cursor
                if (CharacterAtPosition(mousePos, out Character character))
                {
                    SelectCharacter(character);
                }
                // Pick up an item
                else if (selectedCharacter != null && EntityController.Get.GetEntity<IItem>(mousePos, out IItem item))
                {
                    log.Info($"Adding Pickup Order for {item.Name} to {selectedCharacter.Name} @ {mousePos}");
                    PickUpItemTask task = new PickUpItemTask(item.Coordinates, item, selectedCharacter.Inventory);
                    TaskSystem.Assign(task, selectedCharacter, TaskAssignmentMethod.DEFAULT);
                }              
                else if (mousePos != _lastMovePos)
                {
                    // Move the selected character
                    if (selectedCharacter != null)
                    {
                        log.Info($"Force Moving {selectedCharacter.Name} to::{mousePos}");
                        MoveToWaypointTask moveTo = new MoveToWaypointTask(mousePos);
                        TaskSystem.Assign(moveTo, selectedCharacter, TaskAssignmentMethod.INTERRUPT);
                    }
                    // Move any character.
                    else
                    {
                        log.Info($"Adding Move Order To::{mousePos}");
                        MoveToWaypointTask moveTo = new MoveToWaypointTask(mousePos);
                        TaskSystem.Delegate(moveTo);
                    }
                    _lastMovePos = mousePos;
                }
            }
            else if (Data.ID == "InputID.RightMouseButton")
            {
                SelectCharacter(null);
                //Coordinates mousePos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));
                //if (mousePos != _lastWallPos)
                //{
                //    log.Info($"Building Wall @ ::{mousePos}");
                //    AddWall(mousePos);
                //    _lastWallPos = mousePos;
                //}
            }
            
        }

        private bool constructionMode = false;

        public void ToggleConstruction()
        {
            constructionMode = !constructionMode;
            InterfaceController.Get.InventoryMenu.ConstructionModeToggle = constructionMode;
        }

        public void SelectCharacter(Character character)
        {
            if (selectedCharacter == character) return;

            log.Info($"Selected Character::{selectedCharacter?.Name}");

            selectedCharacter = character;
            selectionOverlay.Select(selectedCharacter);

            if (selectedCharacter != null)
            {
                InterfaceController.Get.OpenMenu(InterfaceMenus.InventoryMenu);
                InterfaceController.Get.InventoryMenu.Set(selectedCharacter.Inventory);
            }
            else
            {
                InterfaceController.Get.CloseMenu(InterfaceMenus.InventoryMenu);
            }
        }

        public bool CharacterAtPosition(Coordinates Coordinates, out Character ret)
        {
            ret = null;
            foreach (var character in Characters)
            {
                if(Coordinates.X >= character.Coordinates.X && Coordinates.Y >= character.Coordinates.Y &&
                    Coordinates.X <= character.Bounds.max.X && Coordinates.Y <= character.Bounds.max.Y)
                {
                    ret = character;
                    return true;
                }
            }
            return false;
        }

        private void AddWall(Coordinates Coordinates)
        {
            Wall wall = new Wall(Coordinates);
            Walls.Add(wall);
            OnWallBuilt?.Invoke(wall);
        }
    }

    public interface ISelectable
    {
        Vector2 Position { get; }
    }

    public class SelectionOverlay
    {
        public ISelectable Selected { get; private set; }
        private Sprite2D sprite;

        public SelectionOverlay()
        {
            sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(0, 0, 64, 64));
        }

        public void Draw()
        {
            if (Selected != null)
            {
                SpriteBatchRenderer.Get.DrawSprite(sprite, Selected.Position, 0);
            }
        }

        public void Select(ISelectable selectable)
        {
            Selected = selectable;
        }
    }

    public class CharacterGizmo : IGizmo
    {
        public bool Enabled { get; set; }

        public bool DrawCharacters { get { return _drawCharacters; } set { _drawCharacters = value; } }
        private bool _drawCharacters = false;
        private Color characterOutlineColor => new Color(125, 75, 125, 225);

        public bool DrawCharacterPaths { get { return _drawCharacterPaths; } set { _drawCharacterPaths = value; } }
        private bool _drawCharacterPaths = false;
        private Color characterPathColor => new Color(225, 225, 225, 225);

        public bool drawCharacterNames = true;
        private Color selectedCharacterColor => new Color(225, 75, 125, 225);

        public CharacterGizmo()
        {
            Debug.NewWorldGizmo(this);
        }

        public void Update()
        {

        }


        public void Draw()
        {
            if (_drawCharacters)
            {
                foreach (var character in PlayerController.Get.Characters)
                {
                    Vector2 pos = character.Coordinates;
                    Color color = character == PlayerController.Get.selectedCharacter ? selectedCharacterColor : characterOutlineColor;
;                   DrawUtils.DrawPolygonOutlineToWorld(new Rectangle((int)pos.X, (int)pos.Y, 64, 64), color);
                }

            }
            if (_drawCharacterPaths)
            {
                foreach (var character in PlayerController.Get.Characters)
                {
                    DrawUtils.DrawLineToWorld(character.Position, character.Movement.MovementTarget, characterPathColor);
                }
            }
            if (drawCharacterNames)
            {
                foreach (var character in PlayerController.Get.Characters)
                {
                    Color color = character == PlayerController.Get.selectedCharacter ? selectedCharacterColor : Color.Yellow;
                    DrawUtils.DrawText(character.Name, character.Position, color);
                }
            }
        }
    }

    public class EntityGizmo : IGizmo
    {
        public bool Enabled { get; set; }
        public bool DrawWalls { get { return _drawWalls; } set { _drawWalls = value; SetDrawWalls(); } }
        private bool _drawWalls = false;
        private Color wallOutlineColor => new Color(225, 25, 25, 125);

        private List<Polygon> entityPolygons;

        public EntityGizmo()
        {
            Debug.NewWorldGizmo(this);
        }

        public void Update()
        {

        }

        public void Draw()
        {
            if (_drawWalls)
            {
                foreach (var poly in entityPolygons)
                {
                    DrawUtils.DrawPolygonOutlineToWorld(poly, wallOutlineColor, 1f);
                }
            }
        }

        private void SetDrawWalls()
        {
            if (_drawWalls)
            {
                entityPolygons = new List<Polygon>();
                PlayerController.Get.OnWallBuilt += AddWallOutline;
                foreach (var wall in PlayerController.Get.Walls)
                {
                    AddWallOutline(wall);
                }
            }
            else
            {
                PlayerController.Get.OnWallBuilt -= AddWallOutline;
                entityPolygons = null;
            }
        }

        private void AddWallOutline(Wall wall)
        {
            Vector2[] corners = new Vector2[4];

            corners[0] = new Coordinates(0,0);
            corners[1] = new Coordinates(1, 0);
            corners[2] = new Coordinates(1, 1);
            corners[3] = new Coordinates(0, 1);

            var poly = new Polygon(corners, wall.Coordinates);
            entityPolygons.Add(poly);
        }
    }

}
