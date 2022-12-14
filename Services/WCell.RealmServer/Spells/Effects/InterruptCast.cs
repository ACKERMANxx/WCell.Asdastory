/*************************************************************************
 *
 *   file		: InterruptCast.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2010-01-14 13:00:53 +0100 (to, 14 jan 2010) $

 *   revision		: $Rev: 1192 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using WCell.Constants.Spells;
using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Misc;

namespace WCell.RealmServer.Spells.Effects
{
	public class InterruptCastEffectHandler : SpellEffectHandler
	{
		public InterruptCastEffectHandler(SpellCast cast, SpellEffect effect)
			: base(cast, effect)
		{
		}

		public override SpellFailedReason InitializeTarget(WorldObject target)
		{
			//if (!target.IsUsingSpell)
			//{
			//    return SpellFailedReason.
			//}
			return base.InitializeTarget(target);
		}

		protected override void Apply(WorldObject target, ref DamageAction[] actions)
		{
			if (target.IsUsingSpell)
			{
				target.SpellCast.Cancel(SpellFailedReason.Interrupted);	
			}
		}

		public override ObjectTypes TargetType
		{
			get { return ObjectTypes.Unit; }
		}
	}
}