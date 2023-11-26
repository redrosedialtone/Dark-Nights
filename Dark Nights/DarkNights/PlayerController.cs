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

        private CharacterGizmo characterGizmo;

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
            characterGizmo.DrawPlayer = true;
        }

        public override void Tick()
        {
            base.Tick();
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
            if (PlayerCharacter != null)
            {
                Vector2 pos = Camera.ScreenToWorld(new Vector2(Data.mousePosition.X, Data.mousePosition.Y));
                PlayerCharacter.SetPosition(new Vector2(pos.X, pos.Y));
                log.Info($"Moving Player Character::{PlayerCharacter.Position}");
            }
        }
    }

    public class CharacterGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        public bool DrawPlayer { get { return _drawPlayer; } set { _drawPlayer = value; SetDrawPlayer(); } }
        private bool _drawPlayer = false;
        private Color characterOutlineColor => new Color(125, 75, 125, 225);
        private DrawUtil playerDrawCall;

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
            if (_drawPlayer)
            {
                Vector2[] corners = new Vector2[4];

                corners[0] = WorldSystem.Position(new Vector2(-1,-1));
                corners[1] = WorldSystem.Position(new Vector2(1,-1));
                corners[2] = WorldSystem.Position(new Vector2(1,1));
                corners[3] = WorldSystem.Position(new Vector2(-1,1));

                characterPolygon = new Polygon(corners, PlayerController.Get.PlayerCharacter.Position);
                playerDrawCall += DrawUtils.DrawPolygon(characterPolygon, characterOutlineColor, 3f, drawType: DrawType.World);

                PlayerController.Get.PlayerCharacter.OnEntityMovement += UpdateGizmoPos;
            }
            else
            {
                if (playerDrawCall != null)
                {
                    DrawUtils.RemoveUtil(playerDrawCall);
                    playerDrawCall = null;
                }
            }
        }

        private void UpdateGizmoPos(object sender, EntityMovementArgs e)
        {
            characterPolygon.Position = e.newPosition;
        }
    }

}
