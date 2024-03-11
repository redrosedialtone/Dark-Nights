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

        public Character PlayerCharacter;
        public List<Wall> Walls = new List<Wall>();
        public Action<Wall> OnWallBuilt;

        private CharacterGizmo characterGizmo;
        private EntityGizmo entityGizmo;

        public override void Init()
        {
            log.Info("> ...");
            instance = this;
            PlayerCharacter = new Character((0, 0));

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
        }

        public override void Tick()
        {
            base.Tick();
            PlayerCharacter.Tick();
        }

        public override void Draw()
        {
            base.Draw();
            SpriteBatchRenderer.Get.DrawSprite(PlayerCharacter.Sprite, PlayerCharacter.Position, PlayerCharacter.Rotation);
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
                if (PlayerCharacter != null)
                {
                    Coordinates mousePos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));
                    if (mousePos != _lastMovePos)
                    {
                        PlayerCharacter.Movement.MoveTo(mousePos);
                        log.Info($"Moving Player Character::{PlayerCharacter.Position}");
                        _lastMovePos = mousePos;
                    }

                }
            }
            else if (Data.ID == "InputID.RightMouseButton")
            {
                Coordinates mousePos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));
                if (mousePos != _lastWallPos)
                {
                    AddWall(mousePos);
                    _lastWallPos = mousePos;
                }

            }
            
        }

        private void AddWall(Coordinates Coordinates)
        {
            Wall wall = new Wall(Coordinates);
            Walls.Add(wall);
            OnWallBuilt?.Invoke(wall);
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

        public CharacterGizmo()
        {
            Debug.NewWorldGizmo(this);
        }

        public void Update()
        {

        }


        public void Draw()
        {
            var player = PlayerController.Get.PlayerCharacter;
            if (_drawCharacters)
            {
                Vector2 pos = player.Coordinates;
                DrawUtils.DrawPolygonOutlineToWorld(new Rectangle((int)pos.X, (int)pos.Y, 64,64), characterOutlineColor);
            }
            if (_drawCharacterPaths)
            {
                DrawUtils.DrawLineToWorld(PlayerController.Get.PlayerCharacter.Position, PlayerController.Get.PlayerCharacter.Movement.MovementTarget, characterPathColor);
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
