using vcrossing.Code.Items;

namespace vcrossing.Code.Data;

public interface IEquipableData
{
	Components.Equips.EquipSlot EquipSlot { get; set; }

	// public IEquipable CreateEquipable();
}
