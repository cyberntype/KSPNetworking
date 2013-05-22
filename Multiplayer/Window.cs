using System;

namespace Multiplayer
{
	abstract public class Window
	{
		private Multiplayer user;

		public Window ( Multiplayer user )
		{
			this.user = user;
		}

		abstract public void drawWindow();

	}
}

