using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
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
            Input.Access.AddContext(ctxt);
            Input.Access.EnableContext(ctxt.Name);
            ctxt.OnClick += this.OnClick;

            ApplicationController.Get.Initiate(this);
        }

        public override void OnInitialized()
        {
            base.OnInitialized();

            characterGizmo = new CharacterGizmo();
            characterGizmo.SetDrawGizmo(true);
            characterGizmo.DrawCharacters = true;
            characterGizmo.DrawCharacterPaths = true;

            entityGizmo = new EntityGizmo();
            entityGizmo.SetDrawGizmo(true);
            entityGizmo.DrawWalls = true;
        }

        public override void Tick(Time gameTime)
        {
            base.Tick(gameTime);
            PlayerCharacter.Tick(gameTime);
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

        public void OnClick(MouseButtonActionState Data)
        {
            if (Data.ID == InputID.LeftMouseButton)
            {
                if (PlayerCharacter != null)
                {
                    Vector2 pos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));
                    PlayerCharacter.Movement.MoveTo(pos);
                    log.Info($"Moving Player Character::{PlayerCharacter.Position}");
                }
            }
            else if (Data.ID == InputID.RightMouseButton)
            {
                AddWall(Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y)));
            }
            
        }

        private void AddWall(Coordinates Coordinates)
        {
            Wall wall = new Wall(Coordinates);
            Walls.Add(wall);
            OnWallBuilt?.Invoke(wall);
            NavigationSystem.Get.AddNavNode(wall);
        }
    }

    public class CharacterGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }

        public bool DrawCharacters { get { return _drawCharacters; } set { _drawCharacters = value; SetDrawPlayer(); } }
        private bool _drawCharacters = false;
        private Color characterOutlineColor => new Color(125, 75, 125, 225);
        private DrawUtil characterOutlineDrawCall;
        private Line pathLine;

        public bool DrawCharacterPaths { get { return _drawCharacterPaths; } set { _drawCharacterPaths = value; SetDrawPaths(); } }
        private bool _drawCharacterPaths = false;
        private Color characterPathColor => new Color(225, 225, 225, 225);
        private DrawUtil characterPathDrawCall;



        private Polygon characterPolygon;

        public void DrawGizmos(SpriteBatch Batch)
        {

        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            this.DrawGizmo = drawGizmo;
        }

        private void SetDrawPlayer()
        {
            if (_drawCharacters)
            {
                Vector2[] corners = new Vector2[4];

                corners[0] = new Coordinates(-1,-1);
                corners[1] = new Coordinates(1,-1);
                corners[2] = new Coordinates(1,1);
                corners[3] = new Coordinates(-1,1);

                characterPolygon = new Polygon(corners, PlayerController.Get.PlayerCharacter.Position);
                characterOutlineDrawCall += DrawUtils.DrawPolygonOutline(characterPolygon, characterOutlineColor, 3f, drawType: DrawType.World);

                PlayerController.Get.PlayerCharacter.Movement.OnEntityMovement += UpdateGizmoPos;
            }
            else
            {
                if (characterOutlineDrawCall != null)
                {
                    DrawUtils.RemoveUtil(characterOutlineDrawCall);
                    characterOutlineDrawCall = null;
                }
            }
        }

        private void UpdateGizmoPos(object sender, EntityMovementArgs e)
        {
            if (characterPolygon != null) characterPolygon.Position = e.newPosition;
            if (sender is EntityMovement character)
            {
                pathLine.From = e.newPosition;
                pathLine.To = character.MovementTarget;
            }
        }

        private void SetDrawPaths()
        {
            if (_drawCharacterPaths)
            {
                pathLine = new Line(PlayerController.Get.PlayerCharacter.Position, PlayerController.Get.PlayerCharacter.Movement.MovementTarget);
                characterPathDrawCall += DrawUtils.DrawLine(pathLine, characterPathColor, 1f, drawType: DrawType.World);
            }
            else
            {
                if (characterPathDrawCall != null)
                {
                    DrawUtils.RemoveUtil(characterPathDrawCall);
                    characterPathDrawCall = null;
                }
            }
        }
    }

    public class EntityGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        public bool DrawWalls { get { return _drawWalls; } set { _drawWalls = value; SetDrawWalls(); } }
        private bool _drawWalls = false;
        private Color wallOutlineColor => new Color(225, 25, 25, 225);
        private DrawUtil playerDrawCall;

        private Polygon characterPolygon;

        public void DrawGizmos(SpriteBatch Batch)
        {

        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            this.DrawGizmo = drawGizmo;
        }

        private void SetDrawWalls()
        {
            if (_drawWalls)
            {
                PlayerController.Get.OnWallBuilt += AddWallOutline;
                foreach (var wall in PlayerController.Get.Walls)
                {
                    AddWallOutline(wall);
                }
            }
            else
            {
                if (playerDrawCall != null)
                {
                    DrawUtils.RemoveUtil(playerDrawCall);
                    playerDrawCall = null;
                }
                PlayerController.Get.OnWallBuilt -= AddWallOutline;
            }
        }

        private void AddWallOutline(Wall wall)
        {
            Vector2[] corners = new Vector2[4];

            corners[0] = new Coordinates(0,0);
            corners[1] = new Coordinates(1, 0);
            corners[2] = new Coordinates(1, 1);
            corners[3] = new Coordinates(0, 1);

            characterPolygon = new Polygon(corners, wall.Coordinates);
            playerDrawCall += DrawUtils.DrawPolygonOutline(characterPolygon, wallOutlineColor, 7f, drawType: DrawType.World);
        }
    }

}
