using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.Threading;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;
using Utilities.Hash;

namespace GRF.Hash {
	public enum HashStrategy {
		Quick
	}

	public class FolderHash {
		public Dictionary<string, byte[]> HashContainer(string file, IHash hash) {
		    Dictionary<string, byte[]> hashResult = new Dictionary<string, byte[]>();

		    using (var container = new GrfHolder(file, GrfLoadOptions.Normal)) {
		        GrfThreadPool<FileEntry> threadPool = new GrfThreadPool<FileEntry>();
		        threadPool.Initialize<ThreadScanGrf>(container.Container, container.FileTable.Entries);

		        foreach (var thread in threadPool.Threads.OfType<ThreadScanGrf>()) {
		            thread.Init(hash, null);
		        }

		        threadPool.Start(p => {
		            CLHelper.StringProgress = Path.GetFileName(file) + " " + String.Format("{0:0.0} %", p);
		        }, () => false);

		        foreach (var pair in threadPool.Threads.OfType<ThreadScanGrf>().SelectMany(thread => thread.Hashes)) {
		            hashResult[pair.Key] = pair.Value;
		        }
		    }

		    return hashResult;
		}

		public Dictionary<string, byte[]> HashContainer(ContainerAbstract<FileEntry> container, IHash hash) {
			Dictionary<string, byte[]> hashResult = new Dictionary<string, byte[]>();

			GrfThreadPool<FileEntry> threadPool = new GrfThreadPool<FileEntry>();
			threadPool.Initialize<ThreadScanGrf>(container, container.Table.Entries, 1);

			foreach (var thread in threadPool.Threads.OfType<ThreadScanGrf>()) {
				thread.Init(hash, null);
			}

			threadPool.Start(p => {
				CLHelper.StringProgress = "GRF " + String.Format("{0:0.0} %", p);
			}, () => false);

			foreach (var pair in threadPool.Threads.OfType<ThreadScanGrf>().SelectMany(thread => thread.Hashes)) {
				hashResult[pair.Key] = pair.Value;
			}

			return hashResult;
		}

		public HashObject HashFolder(string folder, string searchPattern, string outputFile, HashStrategy strategy) {
			folder = folder.TrimEnd('\\', '/');

			if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder + " not found");

			QuickHash hash = new QuickHash(new Md5Hash());
			Dictionary<string, byte[]> hashResult = new Dictionary<string, byte[]>();
			int fileCount = 0;

			foreach (string file in Directory.GetFiles(folder, searchPattern, SearchOption.AllDirectories)) {
				string relativePath = file.Substring(folder.Length + 1);

				if (file.IsExtension(".grf", ".rgz", ".gpf", ".thor")) {
					using (var container = new GrfHolder(file, GrfLoadOptions.Normal)) {
						GrfThreadPool<FileEntry> threadPool = new GrfThreadPool<FileEntry>();
						threadPool.Initialize<ThreadScanGrf>(container.Container, container.FileTable.Entries);

						foreach (var thread in threadPool.Threads.OfType<ThreadScanGrf>()) {
							thread.Init(hash, relativePath);
						}

						threadPool.Start(p => {
							CLHelper.StringProgress = relativePath + " " + String.Format("{0:0.0} %", p);
						}, () => false);

						foreach (var pair in threadPool.Threads.OfType<ThreadScanGrf>().SelectMany(thread => thread.Hashes)) {
							hashResult[pair.Key] = pair.Value;
						}
					}
				}
				else {
					if (fileCount % 10 == 0)
						CLHelper.StringProgress = relativePath;

					hashResult[relativePath] = hash.ComputeByteHash(File.ReadAllBytes(file));
					fileCount++;
				}
			}

			CLHelper.StringProgress = "Writing output...";

			HashObject ho = new HashObject();
			ho.HashMethod = new Md5Hash();
			ho.HeadDirectory = folder;

			foreach (var pair in hashResult) {
				ho.Hashes[new TkPath(pair.Key)] = pair.Value;
			}

			ho.ExploreMethod = HashExploreMethod.ByUsingEverything;
			ho.Save(outputFile);

			CLHelper.StringProgress = "Finished";
			return ho;
		}
	}
}
