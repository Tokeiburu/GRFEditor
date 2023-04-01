namespace GRF.FileFormats.StrFormat {
	public interface IStrScript {
		// Tells what to display in the menu item. This value is usually a simple string.
		object DisplayName { get; }

		// This property is used to group the item under a MenuItem. 
		// You can add a script to an existing MenuItem (such as File, Action, etc).
		string Group { get; }

		// The image file to be displayed in the MenuItem. Simply add an image 
		// in the Scripts folder and enter the name in the field, such as "myImage.png". 
		// There are no restrictions on the image size, but icons are usually at 16x16 pixels.
		string Image { get; }

		// The shortcut bound to this command. You can return "null" if you don't want to 
		// set any. Example : "Ctrl-Shift-H"
		string InputGesture { get; }

		// This method is used to validate the input before executing the script. You 
		// can also simply set the code to "return true;" to always activate the script.
		// Normally, an Act file must be opened before executing a script, hence why most
		// of the scripts have "return act != null;" for their precondition check.
		bool CanExecute(Str str, int selectedLayerIndex, int selectedFrameIndex, int[] selectedLayerIndexes, int[] selectedFrameIndexes);

		// This method is where all the operations should be executed. 
		// There are many methods available to you, and you can write your own very easily.
		// The scripting engine uses the C# compiler, allowing you to do almost anything C#
		// can offer. The quickest way to learn the 'language'/methods is to check out the 
		// other scripts. There is also a guide showing common tasks and examples regarding
		// how to write scripts.
		void Execute(Str str, int selectedLayerIndex, int selectedFrameIndex, int[] selectedLayerIndexes, int[] selectedFrameIndexes);
	}
}