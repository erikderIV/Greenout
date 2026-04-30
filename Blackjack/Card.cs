using System;

public class Card
{
    public Suits Suit;
    public int Rank;

    public Card(Suits suits, int rank)
    {
        Suit = suits;
        Rank = rank;
    }
}

public enum Suits
{
    Heart,
    Diamond,
    Club,
    Spade
}