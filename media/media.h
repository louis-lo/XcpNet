// media.h

#pragma once

extern "C"
{
#include <libavformat/avformat.h>
}

#include <Windows.h>

using namespace System;

namespace XcpMedia {

	public ref class MediaPlayer sealed
	{
	public:
		MediaPlayer();
		~MediaPlayer();

		void SetRenderWindow(IntPtr^ hWnd);

		void Play(String^ url);

		void Stop();

	private:
		IntPtr^ m_hWnd;
	};
}
