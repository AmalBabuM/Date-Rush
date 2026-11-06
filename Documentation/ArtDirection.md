# Date Rush artwork

Tool: built-in image generation. Production assets live in `Assets/DateRush/Resources`.

## Market.png

Prompt: Create a production 2D game background asset, landscape 16:9, for Date Rush. Cozy Emirati date market warehouse interior, warm carved walnut wooden walls, geometric Arabic ornament, amber hanging lanterns, sandy plaster lower walls, open arched double wooden doors centered at bottom showing sunlit oasis palms, burlap sacks and date trays at far bottom corners, tiled sandstone floor across bottom 20%. Painterly polished family-friendly mobile game illustration, rich warm browns and gold, soft dramatic lighting, straight-on flat side view. Upper 65% is unobstructed wooden wall with subtle ornament and room for moving conveyor belts to be drawn separately by game code. NO conveyors, NO dates falling, NO characters, NO text, NO signs, NO UI. Keep central lower floor empty for moving player.

## Catcher.png

Prompt: One full-body cheerful Emirati Arab boy age around 10 in white kandura, white ghutra with black agal and brown sandals, large expressive warm brown eyes, friendly smile, brown skin, polished hand-painted cartoon mobile game quality with clean dark outline and soft warm shading. Three-quarter side view facing right, feet both visible, one foot stepping forward, arms reaching forward at waist height as if holding a basket but no basket in this asset. Character fills frame with small even transparent margin, single isolated character on genuinely transparent background. No text, scenery, floor, shadow, extra objects or multiple poses.

## Date0.png through Date5.png

Prompt: Production transparent sprite sheet for a hand painted 2D date-catching family game. Three columns by two rows, each cell containing one large single date fruit centered fully within its cell. Top left Khalas: golden caramel oblong glossy date. Top middle Fardh: dark burgundy oblong date. Top right Khenaizi: deep reddish brown date. Bottom left Lulu: medium brown rounded date. Bottom middle Sukkary: light honey golden crinkled plump date. Bottom right Ajwa: almost black purple rounded wrinkled date. Dates lie horizontally in three-quarter view, softly shaded illustration with visible organic wrinkled skins and warm highlights. No text, labels, dividers, extra objects or drop shadows. Transparent alpha background.

Cleanup prompt: Remove all background and colored glow around these six fruits. Produce transparent cutouts with opaque interiors and anti-aliased edges. Preserve the six fruit objects, colours and regular three-column two-row positions. No glow, haze, shadows, text or scenery.

The resulting atlas was split by grid and fitted into consistent transparent sprite cells for Unity. No source video imagery was used as a game asset.

## Run0.png through Run3.png

Prompt: Create a precise two-by-two animation sheet of the same Emirati boy. Preserve identity, white kandura and ghutra, black agal, brown sandals, large eyes and warm cartoon style. Four full-body right-facing run-cycle poses: right foot forward, passing left leg lifted, left foot forward, passing right leg lifted. Arms extended at waist height, hands empty. Consistent size and alignment. All clothing and feet visible with margins. No basket, floor, shadows, text, labels, dividers or background; transparent alpha outside the characters.

The four poses were split into individual cells and aligned to a common baseline. Unity switches the sprite frames while moving and uses a lifted-leg pose during jumps.
