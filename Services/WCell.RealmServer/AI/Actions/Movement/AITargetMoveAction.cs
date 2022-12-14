using System;
using NLog;
using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.Util.Graphics;

namespace WCell.RealmServer.AI.Actions.Movement
{
	/// <summary>
	/// Lets the owner stay in a specific distance towards a Target
	/// </summary>
	public class AITargetMoveAction : AIAction
	{
		protected static readonly Logger log = LogManager.GetCurrentClassLogger();

		public static int UpdatePositionTicks = 4;

		public static float DefaultFollowDistanceMax = 5f;
		//public static float DefaultFollowDistanceMin = 1f;
		public static float DefaultFollowDistanceMin = 0f;
		public static float DefaultDesiredDistance = 3f;

		protected Unit m_target;

		public AITargetMoveAction(Unit owner)
			: base(owner)
		{
		}

		public virtual float DistanceMin
		{
			get { return DefaultFollowDistanceMin; }
		}

		public virtual float DistanceMax
		{
			get { return DefaultFollowDistanceMax; }
		}

		public virtual float DesiredDistance
		{
			get { return DefaultDesiredDistance; }
		}

		public Unit Target
		{
			get { return m_target; }
			set { m_target = value; }
		}

		public virtual bool IsInRange(WorldObject target)
		{var owner = (NPC)m_owner;
			var distSq = m_owner.GetDistanceSq(target);
            foreach(var s in owner.NPCSpells.ReadySpells)
            {
                if (s.Range.MaxDist * s.Range.MaxDist > distSq)
                    return true;
            }
                return distSq < DistanceMax * DistanceMax ;
		}

		public override void Start()
		{
			if (m_target == null)
			{
				if (m_owner.Target == null)
				{
					log.Error("Started " + GetType().Name + " without Target set: " + m_owner);
					m_owner.Brain.EnterDefaultState();
					return;
				}
				else
				{
					m_target = m_owner.Target;
				}
			}
			Update();
		}

		public override void Update()
		{
            if (m_owner.CastingTill > DateTime.Now)
                return;
			if (m_target == null || !m_target.IsInWorld || m_target.Map!=m_owner.Map)
			{
				// lost target
				OnLostTarget();
				if (m_target == null)
				{
					return;
				}
			}

			if (!m_owner.Movement.Update() && !m_owner.CanMove)
			{
				return;
			}

			if (!m_owner.CanSee(m_target))
			{
				m_owner.Movement.Stop();
			}
            if(UsesSpells && HasSpellReady && m_owner.CanCastSpells && m_owner.NextSpellUpdate < DateTime.Now)
            {
                m_owner.NextSpellUpdate = DateTime.Now.AddMilliseconds(CharacterFormulas.NpcSpellUpdateDelay);
                OnArrived();
                return;
            }
            if (IsInRange(m_target))
			{
			    OnArrived();
			}
		    else if (!m_owner.IsMoving || NextUpdate < DateTime.Now)
		    {
		        MoveToTargetPoint();
		    }
		}

		protected virtual void OnLostTarget()
		{
			//log.Warn(GetType().Name + " is being updated without a Target set: " + m_owner);
			Stop();
			m_owner.Brain.EnterDefaultState();
		}

		protected virtual void OnArrived()
		{
			m_owner.Movement.Stop();
			if (m_target != null)
			{
				m_owner.SetOrientationTowards(m_target);
			}
		}

		protected virtual void OnTimeout()
		{
			m_owner.Brain.EnterDefaultState();
		}

		public override void Stop()
		{
			m_owner.Movement.Stop();
			m_target = null;
		}

		/// <summary>
		/// Gets a preferred point, close to the current target and walks towards it
		/// </summary>
		/// <returns></returns>
		protected virtual void MoveToTargetPoint()
		{
			var bodySize = m_target.BoundingRadius + m_owner.BoundingRadius;

			var direction = m_target.Position - m_owner.Position;

			if (direction == Vector3.Zero)
			{
				direction = Vector3.Right;
			}
			else
			{
				direction.Normalize();
			}

			m_owner.Movement.MoveTo(m_target.Position - direction * (DesiredDistance + bodySize));
		    NextUpdate = DateTime.Now.AddMilliseconds(CharacterFormulas.NpcMoveUpdateDelay);
		}

	    protected DateTime NextUpdate { get; set; }

	    public override UpdatePriority Priority
		{
			get { return UpdatePriority.LowPriority; }
		}
	}
}