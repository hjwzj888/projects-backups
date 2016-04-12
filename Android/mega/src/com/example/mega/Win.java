package com.example.mega;

import java.io.IOException;

import com.example.mega.util.NavigationBarHider;

import android.app.Activity;
import android.content.Intent;
import android.content.res.AssetFileDescriptor;
import android.media.AudioManager;
import android.media.MediaPlayer;
import android.media.MediaPlayer.OnPreparedListener;
import android.os.Bundle;
import android.view.SurfaceHolder;
import android.view.SurfaceView;
import android.view.View;
import android.widget.ImageView;
import android.widget.TableLayout;
import android.widget.TextView;

public class Win extends Activity {	
	
	public enum RANK{
		DIAMOND,GOLD,SILVER,BRONZE
	}
	
	 
    private SurfaceView surfaceview;
    private MediaPlayer mediaPlayer;
 
    private int postion = 0;
    
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		NavigationBarHider.hideNavigationBar(this);
		super.onCreate(savedInstanceState);
		setContentView(R.layout.win);
		
        findViewById();
        initView();
		
		Bundle bundle=this.getIntent().getExtras();
		int steps = bundle.getInt("steps");
		int diffSec = bundle.getInt("diffSec");
		
		TextView text1 = (TextView)findViewById(R.id.text1);
		text1.setText("Total steps:" + steps + "\n" +"Total time:" + diffSec + "sec.");
		
		TextView textRank = (TextView)findViewById(R.id.textRank);
		ImageView imageRank = (ImageView)findViewById(R.id.imageRank);
		switch(calRank(steps,diffSec)){
			case DIAMOND:
				textRank.setText("DIAMOND：You are the god in this game! " +
						"There is nothing that can stop you anymore!");
				imageRank.setImageResource(R.drawable.diamond);
				break;
			case GOLD:
				textRank.setText("GOLD: Now you have been in the top tier. Just one step " +
						"away from mastery.");
				imageRank.setImageResource(R.drawable.gold);
				break;
			case SILVER:
				textRank.setText("SILVER: Practice makes perfact, isn't it? Welcome to " +
						"the advanced level.");
				imageRank.setImageResource(R.drawable.silver);
				break;
			case BRONZE:
				textRank.setText("BRONZE: Greetings, new player. Keep playing and " +
						"you will get it.I promise!");
				imageRank.setImageResource(R.drawable.bronze);
				break;
		}
	}
	
	public void onClick123(View view){		
		Intent intent=new Intent();
		intent.setClass(Win.this,MainActivity.class);						
		startActivity(intent);
	}
	
	public void onClickExit(View view){		
		this.onDestroy();
	}
	
	// Calculate the rank for the player based both on steps and time consumption.
	public RANK calRank(int steps,int time){
		// We assume the standard time for each step is 0.5 sec.
		double score = steps + time*0.5;
		if(score <= 32){
			return RANK.DIAMOND;
		}
		else if(score <= 64){
			return RANK.GOLD;
		}
		else if(score <= 96){
			return RANK.SILVER;
		}
		else{
			return RANK.BRONZE;
		}
	}
	
	 protected void findViewById() {
	        // TODO Auto-generated method stub
	        surfaceview = (SurfaceView) findViewById(R.id.surfaceView);
	    }
	 
	    protected void initView() {
	        // TODO Auto-generated method stub
	        mediaPlayer = new MediaPlayer();
	        surfaceview.getHolder().setKeepScreenOn(true);
	        surfaceview.getHolder().addCallback(new SurfaceViewLis());
	    }
	 
	    private class SurfaceViewLis implements SurfaceHolder.Callback {
	 
	        @Override
	        public void surfaceChanged(SurfaceHolder holder, int format, int width,
	                int height) {
	 
	        }
	 
	        @Override
	        public void surfaceCreated(SurfaceHolder holder) {
	            if (postion == 0) {
	                try {
	                    play();
	                } catch (IllegalArgumentException e) {
	                    // TODO Auto-generated catch block
	                    e.printStackTrace();
	                } catch (SecurityException e) {
	                    // TODO Auto-generated catch block
	                    e.printStackTrace();
	                } catch (IllegalStateException e) {
	                    // TODO Auto-generated catch block
	                    e.printStackTrace();
	                } catch (IOException e) {
	                    // TODO Auto-generated catch block
	                    e.printStackTrace();
	                }
	            }
	        }
	 
	        @Override
	        public void surfaceDestroyed(SurfaceHolder holder) {
	 
	        }
	 
	    }
	 
	    public void play() throws IllegalArgumentException, SecurityException,
	            IllegalStateException, IOException {
	        mediaPlayer.setAudioStreamType(AudioManager.STREAM_MUSIC);
	        AssetFileDescriptor fd = this.getAssets().openFd("light.mp4");
	        mediaPlayer.setDataSource(fd.getFileDescriptor(), fd.getStartOffset(),
	                fd.getLength());
	        mediaPlayer.setLooping(true);
	        mediaPlayer.setDisplay(surfaceview.getHolder());

	        mediaPlayer.prepareAsync();
	        mediaPlayer.setOnPreparedListener(new OnPreparedListener() {
	            @Override
	            public void onPrepared(MediaPlayer mp) {
	                // 装载完毕回调
	                mediaPlayer.start();
	            }
	        });
	    }
}
