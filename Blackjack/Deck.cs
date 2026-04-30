using System;
using System.Collections.Generic;

public class Deck
{
    public List<Card> cards = new List<Card>();

    private Random rnd = new Random();

    public void Initialize()
    {
        cards.Clear();
        for (int j = 0; j < 4; j++)
        {
            Suits suit;
            switch (j)
            {
                case 0: suit = Suits.Heart; break;
                case 1: suit = Suits.Diamond; break;
                case 2: suit = Suits.Club; break;
                case 3: suit = Suits.Spade; break;
            }
            for (int i = 0; i < 13; i++)
            {
                Card card = new Card(suit, i);

                cards.Add(card);
            }
        }
    }

    public Card Draw()
    {
        if (cards.Count == 0) return null;

        int index = rnd.Next(cards.Count);
        Card card = cards[index];

        cards.RemoveAt(index);

        return card;
    }
}
