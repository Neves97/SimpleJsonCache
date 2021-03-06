using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleJSON;

public static class JsonCache {

	#region <<---------- Properties ---------->>
	
	public const string ROOT_FOLDER = "../JsonData/";
	
	#endregion <<---------- Properties ---------->>

	
	
	
	#region <<---------- Public ---------->>
	
	public static async Task SaveJsonAsync(string path, JSONNode jsonNode) {
		if (string.IsNullOrEmpty(path) || jsonNode == null) return;

		var filePathWithExtension = $"{Path.Combine(ROOT_FOLDER, path)}.json";

		// create folder
		Directory.CreateDirectory(Path.GetDirectoryName(filePathWithExtension));

		// write
		await using (StreamWriter writer = File.CreateText(filePathWithExtension)) {
			Console.WriteLine($"Writing file: {filePathWithExtension.Replace(ROOT_FOLDER, string.Empty)}");
			await writer.WriteAsync(jsonNode.ToString(4));
		}
	}

	public static async Task<List<JSONNode>> GetAllJsonInsideFolderAsync(string directoryPath, bool recursive = false, string fileName = "") {
		var jsonNodeList = new List<JSONNode>();
		try {
			directoryPath = Path.Combine(ROOT_FOLDER, directoryPath);
			if (!Directory.Exists(directoryPath))
			{
				throw new Exception("Folder path not found  :" + directoryPath);
			}
			var filesPath = Directory.GetFiles(directoryPath, $"{fileName}*.json", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			foreach (var completeFilePath in filesPath) {
				var filePath = completeFilePath.Substring(ROOT_FOLDER.Length).Replace(".json", string.Empty);
				if (string.IsNullOrEmpty(filePath)) continue;
				filePath = filePath.Replace('\\', '/');
				var json = await LoadJsonAsync(filePath);
				if (json != null) jsonNodeList.Add(json);
			}
			return jsonNodeList;
		} catch (Exception e) {
			Console.WriteLine($"Exception at {nameof(GetAllJsonInsideFolderAsync)}:\n{e}");
		}
		return jsonNodeList;
	}

	public static async Task<JSONNode> LoadJsonAsync(string filePath, TimeSpan maxCacheAge = default){
		try {
			var jsonString = await LoadJsonStringAsync(filePath, maxCacheAge);
			if (string.IsNullOrEmpty(jsonString)) return null;
			return JSON.Parse(jsonString);
		} catch (Exception e) {
			Console.WriteLine(e);
		}
		return string.Empty;
	}

	public static async Task<JSONNode> LoadValueAsync(string filePath, string key) {
		try {
			var jsonString = await LoadJsonStringAsync(filePath);
			if (string.IsNullOrEmpty(jsonString)) return null;
			var jsonNode = JSON.Parse(jsonString);
			return jsonNode[key];
		} catch (Exception e) {
			Console.WriteLine(e);
		}
		return string.Empty;
	}

	public static async Task<bool> DeleteAsync(string path) {
		path = path.Replace('\\', '/').Trim();
		try {
			var filePath = $"{Path.Combine(ROOT_FOLDER, path)}.json";
			if (!File.Exists(filePath)) return false;
			await using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous)) {
				return true;
			}
		} catch (Exception e) {
			Console.WriteLine(e);
		}
		return false;
	}

	/// <summary>
	/// Returns TRUE if the folder is deleted.
	/// </summary>
	public static bool DeleteFolder(string folderPath) {
		try {
			folderPath = Path.GetDirectoryName($"{ROOT_FOLDER}{folderPath}");
			bool isInvalid = Path.GetInvalidPathChars().Any(folderPath.Contains);

			if (isInvalid || string.IsNullOrEmpty(folderPath) || string.IsNullOrWhiteSpace(folderPath)) return false;

			if (Directory.Exists(folderPath)) {
				Directory.Delete(folderPath, true);
				return Directory.Exists(folderPath);
			}
		} catch (Exception e) {
			Console.WriteLine($"Exception at {nameof(DeleteFolder)}: \n{e}");
		}
		return false;
	}
	
	#endregion <<---------- Public ---------->>

	
	
	
	#region <<---------- Private ---------->>
	
	private static async Task<string> LoadJsonStringAsync(string filePath, TimeSpan maxCacheAge = default) {
		filePath = filePath.Replace('\\', '/').Trim();
		try {
			var filePathWithExtension = $"{ROOT_FOLDER}{filePath}.json";
			if (!File.Exists(filePathWithExtension)) return null;

			if (maxCacheAge != default) {

				var info = new FileInfo(filePathWithExtension);
				var cacheAge = DateTime.UtcNow - info.LastWriteTimeUtc;
				if (cacheAge > maxCacheAge) {
					Console.WriteLine($"Deleting cache with age {cacheAge} from path '{filePathWithExtension}'");
					await using (new FileStream(filePathWithExtension, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous)) { }
					return null;
				}
			}
			
			await using (var sourceStream = new FileStream(filePathWithExtension, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
				var sb = new StringBuilder();
				byte[] buffer = new byte[0x1000];
				int numRead = 0;
				while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0) {
					sb.Append(Encoding.UTF8.GetString(buffer, 0, numRead));
				}
				return sb.ToString();
			}
		} catch (Exception e) {
			Console.WriteLine(e);
		}
		return null;
	}
	
	#endregion <<---------- Private ---------->>
	
}
