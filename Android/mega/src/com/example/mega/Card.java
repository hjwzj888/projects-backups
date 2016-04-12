package com.example.mega;

import android.app.Activity;
import android.graphics.drawable.Drawable;
import android.widget.Button;
import android.widget.ImageButton;
import android.view.View;
import android.widget.Toast;


public class Card {	
	// The name of image for this card
	private String img;
	
	// The species of this card
	private String species;
	
	// The evolution level for this card
	private Integer level;	
	
	// Whether the card is face up
	public boolean isFaceUp;
	
	// Store the maxLevel
	public final static Integer maxLevel = 3;
	
	// Whether the card is matched
	public boolean isMatched;
		
	// The constructor
	public Card(String img){
		this.img = img;
		//Level would be the last char of img
		species = img.split("_")[0];
		level = Integer.valueOf(img.split("_")[1]);
		isFaceUp = false;
		isMatched = false;
	}
	
	public Card(String species,int level){
		this.level = level;
		this.species = species;
		img = species+"_"+level;
		isFaceUp = false;
		isMatched = false;
	}
	
	public String getImg(){
		if(isFaceUp == true){
			return img;
		}
		else{
			return "card_back";
		}
	}
	
	public void evolve(){
		level++;
		img = species+"_"+level;
	}
	
	public int getLevel(){
		return level;
	}
	
	public void flip(){
		if(isFaceUp == true){
			isFaceUp = false;
		}
		else{
			isFaceUp = true;
		}
	}	
}
