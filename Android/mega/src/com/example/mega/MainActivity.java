package com.example.mega;

import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.Random;

import com.example.mega.util.NavigationBarHider;

import android.media.MediaPlayer;
import android.os.Build;
import android.os.Bundle;
import android.annotation.TargetApi;
import android.app.Activity;
import android.content.ContextWrapper;
import android.content.Intent;
import android.view.Gravity;
import android.view.Menu;
import android.view.View;
import android.view.View.OnClickListener; 
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.ImageButton;
import android.widget.ImageView;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.Toast;
import android.util.Log;
import android.widget.TableRow.LayoutParams;
import java.util.Date;
import android.text.format.DateFormat;

/**
 * The main activity for this program
 */

public class MainActivity extends Activity {
	// rows*columns should be divisible by 4.
	// Because each species has 3 levels. The number of cards for 
	// each species would be 4(2 cards for the initial level).
	final static int ROWS = 4;
	final static int COLS = 4;
	
	// Dp size for each cards
	final static int dp_width = 70;
	final static int dp_height = 70;
	
	// Store the id of the flipped cards
	ArrayList<Integer> cardsFlipped = new ArrayList<Integer>();
	
	// Store all cards. We use the ID for each button to calculate
	// the corresponding Card.
	Card[][] aCards = new Card[ROWS][COLS];
	
	// Store the steps
	Integer steps;
	
	// Final string for this package name
	final static String THIS_PACKAGE_NAME = "com.example.mega";

	// The sound effect media player
	MediaPlayer evolveSEPlayer;
	
	// The begin time of the game;
	Date beginDate;
	
	private Intent intentBGM = new Intent(THIS_PACKAGE_NAME);
	
	  /**
	   * What the program do on create
	   * @param Bundle savedInstanceState
	   */
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		NavigationBarHider.hideNavigationBar(this);
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
			
		//SimpleDateFormat df = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
		steps = 0;
		beginDate   =   new   Date(System.currentTimeMillis());
		// ---------------The game part starts from here!
		String[] imgNames = initializeNames();
		buildDeck(imgNames);
		buildGrid((TableLayout)findViewById(R.id.canvas));
		
		startService(intentBGM);
	
	}

	  /**
	   * What the program do OptionsMenu
	   * @param Menu menu
	   */
	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.main, menu);
		return true;
	}
	
	  /**
	   * Build the grid of buttons
	   * @param Menu menu
	   */
	public void buildGrid(TableLayout grid){
		
		//grid.setStretchAllColumns(true);    
		
		for(int i = 0;i<ROWS;i++){
			TableRow tableRow = new TableRow(this);
			//tableRow.setGravity(Gravity.CENTER_HORIZONTAL);
			for(int j = 0;j<COLS;j++){
				ImageButton btn = new ImageButton(this);
				int imgID = getResources().getIdentifier(aCards[i][j].getImg(), "drawable", this.getPackageName());
				btn.setImageDrawable(getResources().getDrawable(imgID));
				//btn.setBackground(getResources().getDrawable(R.drawable.button_style));
				// Set the ID of the btn, so we can link it to the corresponding
				// aCards[i][j].
				btn.setId(i*10+j);
			
				btn.setScaleType(ImageView.ScaleType.CENTER_CROP);
				
				// Set width and height
				DensityUtil densityTool = new DensityUtil();
				int px_width = densityTool.dip2px(getApplicationContext(), dp_width);
				int px_height = densityTool.dip2px(getApplicationContext(), dp_height);
				TableRow.LayoutParams lytp = new TableRow.LayoutParams(px_width,px_height);		
				btn.setLayoutParams(lytp);
							
		        btn.setOnClickListener(new OnClickListener() {  
		            @Override  
		            public void onClick(View view) { 
		            	
		            	// If the player clicked on a faced-up card,
		            	// then nothing happens.
		            	Integer i = getRowFromID(view.getId());
				        Integer j = getColFromID(view.getId());
				        if(aCards[i][j].isFaceUp == true){
				        	return;
				        }
		            	
		            	// If there are already two cards flipped, we should
		            	// first do some logic check, then flip them back
		            	            		
		            	if(cardsFlipped.size()==2){
		            		int id1 = cardsFlipped.get(0);
		            		Card card1 = aCards[getRowFromID(id1)][getColFromID(id1)];
		            		
		            		int id2 = cardsFlipped.get(1);
		            		Card card2 = aCards[getRowFromID(id2)][getColFromID(id2)];
		            		
		            		// Since it can't be the same card, we flip them back.
	            			card1.flip();
	            			ImageButton btn = (ImageButton)findViewById(id1);
							int imgID = getResources().getIdentifier(card1.getImg(), "drawable", THIS_PACKAGE_NAME);
							btn.setImageDrawable(getResources().getDrawable(imgID));
							
			    			card2.flip();
	            			btn = (ImageButton)findViewById(id2);
							imgID = getResources().getIdentifier(card2.getImg(), "drawable", THIS_PACKAGE_NAME);
							btn.setImageDrawable(getResources().getDrawable(imgID));
							
							cardsFlipped = new ArrayList();
		            		
		            	}
		            	
		            	// Flip the card at present
		            	cardsFlipped.add(view.getId());
		            		
			            aCards[i][j].flip();
			            ImageButton btn = (ImageButton)view;
						int imgID = getResources().getIdentifier(aCards[i][j].getImg(), "drawable", THIS_PACKAGE_NAME);
						btn.setImageDrawable(getResources().getDrawable(imgID));
						
						// Do post check. If they are the same card, evolve them
						if(cardsFlipped.size()==2){
		            		int id1 = cardsFlipped.get(0);
		            		Card card1 = aCards[getRowFromID(id1)][getColFromID(id1)];
		            		
		            		int id2 = cardsFlipped.get(1);
		            		Card card2 = aCards[getRowFromID(id2)][getColFromID(id2)];

		            		if(card1.getImg().equals(card2.getImg())){
		            			// We may add card to the cardsFlipped array
		            			// in the evolution, so we initialize the array first.
			            		cardsFlipped = new ArrayList();
		            			evolve(id1,id2);
		            		}		            		
						}
						steps++;
						//If all cards are matched up, then show the win screen
						for(int m=0; m<ROWS; m++){
							for(int n=0; n<COLS; n++){
								if(aCards[m][n].isMatched == false){
									return;
								}
							}
						}
						//Jump to the next activity	
						Date   endDate   =   new   Date(System.currentTimeMillis());
						long diff = endDate.getTime() - beginDate.getTime();
						int diffSec = (int)(diff/1000);
						Intent intent=new Intent();
						intent.setClass(MainActivity.this,Win.class);						
						Bundle bundle=new Bundle();
						bundle.putInt("steps",steps);
						bundle.putInt("diffSec",diffSec);
						intent.putExtras(bundle);
						startActivity(intent);
		            }  
		        });
			      			
				tableRow.addView(btn);
			}
			grid.addView(tableRow);
		}
	}
	
	  /**
	   * Build the deck
	   * @param String[] the names of all images
	   */
	public void buildDeck(String[] imgNames){
		for(int i = 0; i<ROWS; i++){
			for(int j = 0; j<COLS; j++){
				aCards[i][j] = new Card(imgNames[i*COLS + j]);
			}
		}
	}
	
	  /**
	   * Get the row in aCards[] from a button's ID
	   * @param int the id of the button
	   * @return int the row in aCards[]
	   */
	public int getRowFromID(int id){
		return id/10;
	}
	
	  /**
	   * Get the column in aCards[] from a button's ID
	   * @param int the id of the button
	   * @return int the column in aCards[]
	   */
	public int getColFromID(int id){
		return id%10;
	}
	
	  /**
	   * Initialize all names then shuffle them.
	   * @return String[] all names.
	   */
	public String[] initializeNames(){
		String[] imgNames = new String[ROWS*COLS];
		//Manually add image names
		int i = 0;
		
		imgNames[i++] = "fushigidane_1";
		imgNames[i++] = "fushigidane_1";
		imgNames[i++] = "fushigidane_2";
		imgNames[i++] = "fushigidane_3";
		
		imgNames[i++] = "hitokage_1";
		imgNames[i++] = "hitokage_1";
		imgNames[i++] = "hitokage_2";
		imgNames[i++] = "hitokage_3";
		
		imgNames[i++] = "poppo_1";
		imgNames[i++] = "poppo_1";
		imgNames[i++] = "poppo_2";
		imgNames[i++] = "poppo_3";
		
		imgNames[i++] = "zenigame_1";
		imgNames[i++] = "zenigame_1";
		imgNames[i++] = "zenigame_2";
		imgNames[i++] = "zenigame_3";
		
		shuffle(imgNames);
			
		return imgNames;		
	}
	
	  /**
	   * Shuffle the array.
	   * @param String[] The array to be shuffled.
	   */
	public void shuffle(String[] imgNames){
		// We use an easy way to shuffle that we swap every elements with a random 
		// elements.
		int length = imgNames.length;
		Random random = new Random();
		for(int i=0;i<length;i++){
			
			int j = random.nextInt(length);
			String temp = imgNames[i];
			imgNames[i] = imgNames[j];
			imgNames[j] = temp;
		}
	}
	
	  /**
	   * Evolve the card.
	   * @param int The id of card 1.
	   * @param int The id of card 2.
	   */
	public void evolve(int id1,int id2){		
		Card card1 = aCards[getRowFromID(id1)][getColFromID(id1)];		
		Card card2 = aCards[getRowFromID(id2)][getColFromID(id2)];

		// If they are the highest level, vanish them
		if(card1.getLevel() == Card.maxLevel){
			vanish(id1);
			vanish(id2);
		}
		else{
			Toast.makeText(MainActivity.this,"Evolved!",Toast.LENGTH_SHORT).show();
			fuse(id1,id2);
		}
	}
	
	  /**
	   * Vanish a card.
	   * @param int The id of the card.
	   */
	public void vanish(int id){
		Card card = aCards[getRowFromID(id)][getColFromID(id)];
		card.isMatched = true;
		
		ImageButton btn = (ImageButton)findViewById(id);
		btn.setVisibility(View.INVISIBLE);
		
		vanishAnimation(btn);
	}
	

	public void vanishAnimation(ImageButton btn){
		
	}
	
	  /**
	   * Fuse two cards. If the reaches the max level, vanish them. Otherwise
	   * evolve them.
	   * @param int The id of card 1.
	   * @param int The id of card 2.
	   */
	public void fuse(int id1,int id2){
		Card card1 = aCards[getRowFromID(id1)][getColFromID(id1)];		
		Card card2 = aCards[getRowFromID(id2)][getColFromID(id2)];  
		
		// Basically, we want to hide id1 which is the first tapped card.
		// So the player can keep tapping the new card at id2, without 
		// extra finger moves.
		card1.isMatched = true;
		ImageButton btn1 = (ImageButton)findViewById(id1);
		btn1.setVisibility(View.INVISIBLE);
		
		// Fuse a new level card into card2;
		card2.evolve();
		//card2.flip();
		ImageButton btn2 = (ImageButton)findViewById(id2);
		int imgID = getResources().getIdentifier(card2.getImg(), "drawable", THIS_PACKAGE_NAME);
		btn2.setImageDrawable(getResources().getDrawable(imgID));
		cardsFlipped.add(id2);
	}
	
	public void fuseAnimation(ImageButton btn1,ImageButton btn2){
		
	}

	  /**
	   * What the program do on destroy
	   */
    protected void onDestroy() {
//    	if(intentBGM!=null){
//    		stopService(intentBGM);
//    	}
        super.onDestroy();
    }
    
    /**
	   * Restart the game
	   * @param View view
	   */
    public void onClick_restart(View view){
		Intent intent = getIntent();
		overridePendingTransition(0, 0);
		intent.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION);
		finish();

		overridePendingTransition(0, 0);
		startActivity(intent);
    }
    
    /**
	   * Change the music randomly.
	   * @param View view
	   */
    public void onClick_changeMusic(View view){
 		stopService(intentBGM);
 		startService(intentBGM);
     }
}
