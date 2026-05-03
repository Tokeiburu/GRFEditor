using System.Collections.Generic;

namespace GrfToWpfBridge.DrawingComponents {
	public interface IDrawingModule {
		int DrawingPriority { get; }
		List<DrawingComponent> GetComponents();
		bool Permanent { get; }
	}
}
