/*************************************************************************
 *
 *   file		: SchoolImmunity.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2010-01-14 20:02:32 +0100 (to, 14 jan 2010) $

 *   revision		: $Rev: 1194 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using NLog;

namespace WCell.RealmServer.Spells.Auras.Handlers
{
	public class StateImmunityHandler : AuraEffectHandler
	{
		static Logger log = LogManager.GetCurrentClassLogger();

		protected override void Apply()
		{
		}

		protected override void Remove(bool cancelled)
		{
		}
	}
};