# GRFEditor
An editor for the GRF/GPF/Thor file formats from Ragnarok Online.

### Using the GRF library
#### Setting up the ErrorHandler
With a new project, the first thing you'll want to do is look into the ErrorHandler.
By default, the ErrorHandler is set to use ErrorManager.DefaultHandler which uses the Console window and may not be very helpful.
GRF Editor uses GrfToWpfBridge.Application.DefaultErrorHandler which uses a WPF based interface.
If you want to handle the exceptions yourself, you can simply rethrow them with the RethrowErrorHandler shown below (or make your own).
```
public class RethrowErrorHandler : IErrorHandler {
	public void Handle(Exception exception, ErrorLevel errorLevel) {
		throw exception;
	}

	public bool YesNoRequest(string message, string caption) {
		if (MessageBox.Show("The application requires your attention.\n\n" + message, caption, MessageBoxButtons.YesNo) == DialogResult.Yes)
			return true;

		return false;
	}
}

public class GrfTest {
	public GrfTest() {
		ErrorHandler.SetErrorHandler(new DefaultHandler());
		ErrorHandler.SetErrorHandler(new RethrowErrorHandler());
		ErrorHandler.SetErrorHandler(new DefaultErrorHandler());
	}
}
```
#### Using GrfHolder
The GrfHolder is the main class for handling GRF files. All operations on the GRF must be applied by using the Commands object. You can find all the available methods in GRF.ContainerFormat.Commands.CommandsHolder.cs.
```
// Add and remove files
GrfHolder grf = new GrfHolder(@"C:\data.grf");

grf.Commands.AddFile(@"data\texture\grid.tga", @"C:\test\custom_grid.tga");
grf.Commands.RemoveFile(@"data\texture\loading00.jpg");

if (grf.Commands.CanUndo)
	grf.Commands.Undo();

if (grf.Commands.CanRedo)
	grf.Commands.Redo();

grf.QuickSave();
// Reload is only necessary if you plan on using the GRF again after saving it.
grf.Reload();
```

The entries from the GRF are stored in the FileTable object and can be extracted as follows:
```
GrfHolder grf = new GrfHolder(@"C:\data.grf");

var entry = grf.FileTable.TryGet(@"data\texture\grid.tga");

if (entry == null)
	throw new Exception("Entry not found.");

// You can also get the entry directly as follow
entry = grf.FileTable[@"data\texture\grid.tga"];

var data = entry.GetDecompressedData();

File.WriteAllBytes(@"C:\test\custom_grid.tga", data);
```

You can also iterate through a specific folder in the GRF.
```
GrfHolder grf = new GrfHolder(@"C:\data.grf");

foreach (var entry in grf.FileTable.EntriesInDirectory(@"C:\texture", SearchOption.TopDirectoryOnly)) {
	if (entry.RelativePath.IsExtension(".bmp")) {
		// Using GrfImage requires additional configuration, more on that later.
		GrfImage image = new GrfImage(entry);

		image.Convert(GrfImageType.Bgra32);
		image.Save(GrfPath.Combine(@"C:\test\", entry.RelativePath));
	}
}
```
