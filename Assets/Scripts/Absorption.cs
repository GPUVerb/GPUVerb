namespace GPUVerb
{
	public enum AbsorptionCoefficient
	{
		FreeSpace,
		Default,
		BrickUnglazed,
		BrickPainted,
		ConcreteRough,
		ConcreteBlockPainted,
		GlassHeavy,
		GlassWindow,
		TileGlazed,
		PlasterBrick,
		PlasterConcreteBlock,
		WoodPlywoodPanel,
		Steel,
		WoodPanel,
		ConcreteBlockCoarse,
		DraperyLight,
		DraperyMedium,
		DraperyHeavy,
		FiberboardShreddedWood,
		ConcretePainted,
		Wood,
		WoodVarnished,
		CarpetHeavy,
		Gravel,
		Grass,
		SnowFresh,
		SoilRough,
		WoodTree,
		WaterSurface,
		Concrete,
		Glass,
		Marble,
		Drapery,
		Cloth,
		Awning,
		Foliage,
		Metal,
		Ice,

		Count
	}
	public class AbsorptionConstants
    {
		private static double[] s_absorptionCoefficients =
		{
			0.0000000000,	// FreeSpace,
			0.9899494940,	// Default,
			0.9797958970,	// BrickUnglazed,
			0.9899494940,	// BrickPainted,
			0.9695359710,	// ConcreteRough,
			0.9643650760,	// ConcreteBlockPainted,
			0.9848857800,	// GlassHeavy,
			0.9380831520,	// GlassWindow,
			0.9949874370,	// TileGlazed,
			0.9848857800,	// PlasterBrick,
			0.9746794340,	// PlasterConcreteBlock,
			0.9486832980,	// WoodPlywoodPanel,
			0.9486832980,	// Steel,
			0.9539392010,	// WoodPanel,
			0.8062257750,	// ConcreteBlockCoarse,
			0.9219544460,	// DraperyLight,
			0.6708203930,	// DraperyMedium,
			0.6324555320,	// DraperyHeavy,
			0.6324555320,	// FiberboardShreddedWood,
			0.9899494940,	// ConcretePainted,
			0.9643650760,	// Wood,
			0.9848857800,	// WoodVarnished,
			0.8062257750,	// CarpetHeavy,
			0.5477225580,	// Gravel,
			0.5477225580,	// Grass,
			0.3162277660,	// SnowFresh,
			0.7416198490,	// SoilRough,
			0.9110433580,	// WoodTree,
			0.9949874370,	// WaterSurface,
			0.9797958970,	// Concrete,
			0.9695359710,	// Glass,
			0.9949874370,	// Marble,
			0.9219544460,	// Drapery,
			0.9219544460,	// Cloth,
			0.9219544460,	// Awning,
			0.9110433580,	// Foliage,
			0.9486832980,	// Metal,
			0.9949874370	// Ice,
		};

		public static double GetAbsorption(AbsorptionCoefficient type)
        {
			return s_absorptionCoefficients[(int)type];
        }
	}

}
