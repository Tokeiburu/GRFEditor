namespace GRF.FileFormats.GndFormat {
	//public class LightmapContainer {
	//	private byte[] _data;
	//	private int[] _indices;
	//	private  Stack<int> _freeSlots;
	//	private int _count;
	//	private int _capacity;
	//
	//	private int _lightmapSize;
	//
	//	public LightmapContainer() {
	//	}
	//
	//	public LightmapContainer(byte[] data, int lightmapSize) {
	//		SetData(data, lightmapSize);
	//	}
	//
	//	public void SetData(byte[] data, int lightmapSize) {
	//		_data = data;
	//		_lightmapSize = lightmapSize;
	//
	//		_count = _data.Length / _lightmapSize;
	//		_capacity = _count;
	//		_indices = new int[_count];
	//
	//		for (int i = 0; i < _count; i++)
	//			_indices[i] = i;
	//	}
	//
	//	public void Clear() {
	//		_freeSlots.Clear();
	//
	//		for (int i = 0; i < _indices.Length; i++) {
	//			_indices[i] = -1;
	//			_freeSlots.Push(i);
	//		}
	//
	//		_count = 0;
	//	}
	//
	//	public int AddLightmap(byte[] data) {
	//		if (_freeSlots.Count == 0)
	//			Grow();
	//
	//		int index = _freeSlots.Pop();
	//		_indices[index] = index;
	//		Buffer.BlockCopy(data, 0, _data, index * _lightmapSize, _lightmapSize);
	//		_count++;
	//		return index;
	//	}
	//
	//	public void Grow() {
	//		int newCapacity = _capacity == 0 ? 1024 : _capacity * 2;
	//
	//		byte[] newData = new byte[newCapacity * _lightmapSize];
	//		int[] newIndices = new int[newCapacity];
	//		Buffer.BlockCopy(_data, 0, newData, 0, _capacity * _lightmapSize);
	//		Buffer.BlockCopy(_indices, 0, newIndices, 0, _capacity);
	//
	//		_data = newData;
	//		_capacity = newCapacity;
	//
	//		for (int i = _capacity; i < newCapacity; i++) {
	//			_freeSlots.Push(i);
	//		}
	//	}
	//
	//	public void RemoveAt(int index) {
	//		ValidateIndex(index);
	//
	//		_freeSlots.Push(index);
	//		_indices[index] = -1;
	//		_count--;
	//	}
	//
	//	public byte[] GetLightmap(int index) {
	//		ValidateIndex(index);
	//
	//		byte[] data = new byte[_lightmapSize];
	//		Buffer.BlockCopy(_data, _indices[index] * _lightmapSize, data, 0, _lightmapSize);
	//		return data;
	//	}
	//
	//	public unsafe byte* GetLightmapPtr(int index) {
	//		ValidateIndex(index);
	//
	//		fixed (byte* p = _data) {
	//			return p + _indices[index] * _lightmapSize;
	//		}
	//	}
	//
	//	public void ValidateIndex(int index) {
	//		if (index < 0 || index >= _capacity || _indices[index] == -1)
	//			throw new IndexOutOfRangeException("index");
	//	}
	//
	//	public void RemoveRange(int start, int count) {
	//		//
	//	}
	//}
}
