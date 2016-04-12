package com.example.mega;

import java.util.Random;

import android.app.Service;
import android.content.Intent;
import android.media.MediaPlayer;
import android.os.IBinder;
public class MusicServer extends Service {
	
	private final static Integer MUSIC_NUMBER = 5;
	
	private MediaPlayer mediaPlayer;
	
	@Override
	public IBinder onBind(Intent intent) {
		// TODO Auto-generated method stub
		return null;
	}
		@Override
		public void onStart(Intent intent,int startId){
		super.onStart(intent, startId);
		if(mediaPlayer==null){
		
	    Random random = new Random();
	    Integer num = random.nextInt(MUSIC_NUMBER)+1;
	    String musicName = "bgm" + num;
	    
		int musicID = getResources().getIdentifier(musicName, "raw", this.getPackageName());
		mediaPlayer = MediaPlayer.create(this, musicID);
		mediaPlayer.setLooping(true);
		mediaPlayer.start();
		}
	}
		
	@Override
	public void onDestroy() {
		// TODO Auto-generated method stub
		super.onDestroy();
		mediaPlayer.stop();
	}
}