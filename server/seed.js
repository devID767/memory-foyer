const CAPITALS = [
    ['Austria', 'Vienna'],
    ['Belgium', 'Brussels'],
    ['Bulgaria', 'Sofia'],
    ['Croatia', 'Zagreb'],
    ['Cyprus', 'Nicosia'],
    ['Czechia', 'Prague'],
    ['Denmark', 'Copenhagen'],
    ['Estonia', 'Tallinn'],
    ['Finland', 'Helsinki'],
    ['France', 'Paris'],
    ['Germany', 'Berlin'],
    ['Greece', 'Athens'],
    ['Hungary', 'Budapest'],
    ['Ireland', 'Dublin'],
    ['Italy', 'Rome'],
    ['Latvia', 'Riga'],
    ['Lithuania', 'Vilnius'],
    ['Luxembourg', 'Luxembourg City'],
    ['Malta', 'Valletta'],
    ['Netherlands', 'Amsterdam'],
    ['Poland', 'Warsaw'],
    ['Portugal', 'Lisbon'],
    ['Romania', 'Bucharest'],
    ['Slovakia', 'Bratislava'],
    ['Slovenia', 'Ljubljana'],
    ['Spain', 'Madrid'],
    ['Sweden', 'Stockholm'],
    ['United Kingdom', 'London'],
    ['Norway', 'Oslo'],
    ['Switzerland', 'Bern'],
    ['Iceland', 'Reykjavik'],
    ['Albania', 'Tirana'],
    ['Andorra', 'Andorra la Vella'],
    ['Belarus', 'Minsk'],
    ['Bosnia and Herzegovina', 'Sarajevo'],
    ['Liechtenstein', 'Vaduz'],
    ['Moldova', 'Chișinău'],
    ['Monaco', 'Monaco'],
    ['Montenegro', 'Podgorica'],
    ['North Macedonia', 'Skopje'],
    ['San Marino', 'San Marino'],
    ['Serbia', 'Belgrade'],
    ['Ukraine', 'Kyiv'],
    ['Vatican City', 'Vatican City'],
];

const ELEMENTS = [
    ['1', 'Hydrogen (H)'],
    ['2', 'Helium (He)'],
    ['3', 'Lithium (Li)'],
    ['4', 'Beryllium (Be)'],
    ['5', 'Boron (B)'],
    ['6', 'Carbon (C)'],
    ['7', 'Nitrogen (N)'],
    ['8', 'Oxygen (O)'],
    ['9', 'Fluorine (F)'],
    ['10', 'Neon (Ne)'],
    ['11', 'Sodium (Na)'],
    ['12', 'Magnesium (Mg)'],
    ['13', 'Aluminium (Al)'],
    ['14', 'Silicon (Si)'],
    ['15', 'Phosphorus (P)'],
    ['16', 'Sulfur (S)'],
    ['17', 'Chlorine (Cl)'],
    ['18', 'Argon (Ar)'],
    ['19', 'Potassium (K)'],
    ['20', 'Calcium (Ca)'],
];

const IDIOMS = [
    ['Break the ice', 'Initiate conversation in a social setting'],
    ['Hit the books', 'Study hard'],
    ['Bite the bullet', 'Endure something painful or unpleasant'],
    ['Piece of cake', 'Something very easy'],
    ['Under the weather', 'Feeling ill'],
    ['Cost an arm and a leg', 'Be very expensive'],
    ['Spill the beans', 'Reveal a secret'],
    ['Once in a blue moon', 'Very rarely'],
    ['Let the cat out of the bag', 'Disclose a secret unintentionally'],
    ['Burn the midnight oil', 'Work late into the night'],
    ['Cut corners', 'Do something poorly to save time or money'],
    ['Hit the nail on the head', 'Describe a situation exactly'],
    ['Pull someone’s leg', 'Tease or joke with someone'],
    ['The best of both worlds', 'Enjoy two different opportunities at once'],
    ['Speak of the devil', 'The person we were just discussing has appeared'],
    ['When pigs fly', 'Something that will never happen'],
    ['Add insult to injury', 'Make a bad situation worse'],
    ['Beat around the bush', 'Avoid the main topic'],
    ['Call it a day', 'Stop working on something'],
    ['Get out of hand', 'Become uncontrollable'],
    ['Go the extra mile', 'Do more than is expected'],
    ['Miss the boat', 'Lose an opportunity'],
    ['No pain, no gain', 'Effort is required to achieve results'],
    ['On the ball', 'Alert and competent'],
    ['Pull yourself together', 'Calm down and regain composure'],
    ['So far so good', 'Things are going well so far'],
    ['Take it with a grain of salt', 'Treat a claim skeptically'],
    ['The ball is in your court', 'It is your decision now'],
    ['Through thick and thin', 'In good times and bad'],
    ['Time flies', 'Time passes quickly'],
];

const DECKS = [
    {
        deckId: 'capitals-eu',
        displayName: 'Capitals of Europe',
        description: '44 European countries and their capital cities.',
        newCardsPerDay: 10,
        cards: CAPITALS,
    },
    {
        deckId: 'periodic-1-20',
        displayName: 'Periodic Table 1–20',
        description: 'The first 20 chemical elements by atomic number.',
        newCardsPerDay: 5,
        cards: ELEMENTS,
    },
    {
        deckId: 'english-idioms',
        displayName: 'English Idioms',
        description: '30 common English idioms and their meanings.',
        newCardsPerDay: 8,
        cards: IDIOMS,
    },
];

export function seed(db) {
    const insertDeck = db.prepare(
        'INSERT INTO decks (deck_id, display_name, description, new_cards_per_day) VALUES (?, ?, ?, ?)'
    );
    const insertCard = db.prepare(
        'INSERT INTO cards (card_id, deck_id, front, back, ord) VALUES (?, ?, ?, ?, ?)'
    );
    const insertSchedule = db.prepare(
        `INSERT INTO card_schedules (card_id, reps, ease_factor, interval_days, due_at, stage, learning_step)
         VALUES (?, 0, 2.5, 0, ?, 'new', 0)`
    );

    const now = new Date().toISOString();

    const tx = db.transaction(() => {
        for (const deck of DECKS) {
            insertDeck.run(deck.deckId, deck.displayName, deck.description, deck.newCardsPerDay);
            for (let i = 0; i < deck.cards.length; i++) {
                const ord = i + 1;
                const cardId = `${deck.deckId}:${ord}`;
                const [front, back] = deck.cards[i];
                insertCard.run(cardId, deck.deckId, front, back, ord);
                insertSchedule.run(cardId, now);
            }
        }
    });
    tx();
}
