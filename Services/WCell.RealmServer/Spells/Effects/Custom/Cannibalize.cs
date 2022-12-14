using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Misc;

namespace WCell.RealmServer.Spells.Effects.Custom
{
	/// <summary>
	/// Have an undead feed of flesh
	/// </summary>
	public class CannibalizeEffectHandler : SpellEffectHandler
	{
		public CannibalizeEffectHandler(SpellCast cast, SpellEffect effect)
			: base(cast, effect)
		{
		}

		protected override void Apply(WorldObject target, ref DamageAction[] actions)
		{
			var cast = m_cast;
			if (cast != null)
			{
				var caster = cast.CasterObject;
				//((Unit)caster).EmoteState
			}
		}

		public override ObjectTypes CasterType
		{
			get { return ObjectTypes.Unit; }
		}
	}
}