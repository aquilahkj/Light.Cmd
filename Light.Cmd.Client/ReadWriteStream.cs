using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Light.Cmd.Client
{
	public class ReadWriteStream : Stream
	{
		static readonly int MIN_BUFFER_SIZE = 256;

		int _length;
		byte[] _internalBuffer;
		bool _streamClosed;
		int _position;


		public ReadWriteStream ()
			: this (MIN_BUFFER_SIZE)
		{
		}

		public ReadWriteStream (int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ("capacity");
			_internalBuffer = new byte[capacity];
		}

		void CheckIfClosedThrowDisposed ()
		{
			if (_streamClosed)
				throw new ObjectDisposedException ("Stream");
		}

		public override bool CanRead {
			get {
				return !_streamClosed;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return !_streamClosed;
			}
		}

		public override long Length {
			get {
				CheckIfClosedThrowDisposed ();
				return _length - _position;
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException ();
			}

			set {
				throw new NotSupportedException ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			_streamClosed = true;
			_internalBuffer = null;
		}

		public override void Flush ()
		{
			_position = 0;
			_length = 0;
		}

		public override int Read ([In, Out] byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException ("offset or count less than zero.");

			if (buffer.Length - offset < count)
				throw new ArgumentException ("offset+count",
					"The size of the buffer is less than offset + count.");

			CheckIfClosedThrowDisposed ();

			if (_position >= _length || count == 0)
				return 0;

			if (_position > _length - count)
				count = _length - _position;

			Buffer.BlockCopy (_internalBuffer, _position, buffer, offset, count);
			_position += count;
			return count;
		}

		public override int ReadByte ()
		{
			CheckIfClosedThrowDisposed ();
			if (_position >= _length)
				return -1;

			return _internalBuffer [_position++];
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
		}

		int CalculateNewCapacity (int minimum)
		{
			if (minimum < MIN_BUFFER_SIZE)
				minimum = MIN_BUFFER_SIZE; // See GetBufferTwo test
			int capacity = _internalBuffer.Length * 2;
			if (minimum < capacity)
				minimum = capacity;

			return minimum;
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public virtual byte[] ToArray ()
		{
			int len = _length - _position;
			byte[] outBuffer = new byte[len];
			Buffer.BlockCopy (_internalBuffer, _position, outBuffer, 0, len);
			return outBuffer;
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException ();

			if (buffer.Length - offset < count)
				throw new ArgumentException ("offset+count",
					"The size of the buffer is less than offset + count.");

			CheckIfClosedThrowDisposed ();
			CheckNewData (count);
			Buffer.BlockCopy (buffer, offset, _internalBuffer, _length, count);
			_length += count;
		}

		public override void WriteByte (byte value)
		{
			CheckIfClosedThrowDisposed ();
			CheckNewData (1);
			_length++;
			_internalBuffer [_length] = value;
		}

		void CheckNewData (int value)
		{
			if (value + _length > _internalBuffer.Length) {
				if (_position >= value * 2 + MIN_BUFFER_SIZE) {
					int len = _length - _position;
					Buffer.BlockCopy (_internalBuffer, _position, _internalBuffer, 0, len);
					_length = len;
					_position = 0;
				}
				else {
					int newCap = CalculateNewCapacity (value + _length);
					byte[] newBuffer = new byte[newCap];
					int len = _length - _position;
					Buffer.BlockCopy (_internalBuffer, _position, newBuffer, 0, len);
					_internalBuffer = newBuffer;
					_length = len;
					_position = 0;
				}
			}
		}

		public virtual void WriteTo (Stream stream)
		{
			CheckIfClosedThrowDisposed ();

			if (stream == null)
				throw new ArgumentNullException ("stream");

			stream.Write (_internalBuffer, _position, _length - _position);
		}
	}
}
