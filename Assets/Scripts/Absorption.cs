namespace GPUVerb
{
	public enum AbsorptionCoefficient
	{
		Vacuum,
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
		private static float[] s_absorptionCoefficients =
		{
			0.000000000F,	// FreeSpace,
			0.989949494F,	// Default,
			0.979795897F,	// BrickUnglazed,
			0.989949494F,	// BrickPainted,
			0.969535971F,	// ConcreteRough,
			0.964365076F,	// ConcreteBlockPainted,
			0.984885780F,	// GlassHeavy,
			0.938083152F,	// GlassWindow,
			0.994987437F,	// TileGlazed,
			0.984885780F,	// PlasterBrick,
			0.974679434F,	// PlasterConcreteBlock,
			0.948683298F,	// WoodPlywoodPanel,
			0.948683298F,	// Steel,
			0.953939201F,	// WoodPanel,
			0.806225775F,	// ConcreteBlockCoarse,
			0.921954446F,	// DraperyLight,
			0.670820393F,	// DraperyMedium,
			0.632455532F,	// DraperyHeavy,
			0.632455532F,	// FiberboardShreddedWood,
			0.989949494F,	// ConcretePainted,
			0.964365076F,	// Wood,
			0.984885780F,	// WoodVarnished,
			0.806225775F,	// CarpetHeavy,
			0.547722558F,	// Gravel,
			0.547722558F,	// Grass,
			0.316227766F,	// SnowFresh,
			0.741619849F,	// SoilRough,
			0.911043358F,	// WoodTree,
			0.994987437F,	// WaterSurface,
			0.979795897F,	// Concrete,
			0.969535971F,	// Glass,
			0.994987437F,	// Marble,
			0.921954446F,	// Drapery,
			0.921954446F,	// Cloth,
			0.921954446F,	// Awning,
			0.911043358F,	// Foliage,
			0.948683298F,	// Metal,
			0.994987437F	// Ice,
		};

		public static float GetAbsorption(AbsorptionCoefficient type)
        {
			return s_absorptionCoefficients[(int)type];
        }
	}

}
