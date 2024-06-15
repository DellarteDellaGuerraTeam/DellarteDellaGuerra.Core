﻿using System.Collections.Generic;
using DellarteDellaGuerra.Domain.Equipment.List.Model;

namespace DellarteDellaGuerra.Domain.Equipment.List.Util
{
    public interface IListCivilianEquipment
    {
        IDictionary<string, IList<EquipmentPool>> ListCivilianEquipmentPools();
    }
}