\ ==============================================================================
\
\             str - the character string module in the ffl
\
\               Copyright (C) 2005  Dick van Oudheusden
\  
\ This library is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public
\ License as published by the Free Software Foundation; either
\ version 2 of the License, or (at your option) any later version.
\
\ This library is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
\ General Public License for more details.
\
\ You should have received a copy of the GNU General Public
\ License along with this library; if not, write to the Free
\ Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
\
\ ==============================================================================
\ 
\  $Date: 2006-02-03 19:17:34 $ $Revision: 1.14 $
\
\ ==============================================================================

include ffl/config.fs


[UNDEFINED] str.version [IF]


include ffl/stc.fs
include ffl/chr.fs


( str = Character string )
( The str module implements a text string.)


1 constant str.version


( Public structure )

struct: str%       ( - n = Get the required space for the str data structure )
  cell: str>data
  cell: str>length
  cell: str>size    
  cell: str>extra
;struct


( Private database )

8 value str.extra   ( - w = the initial extra space )


( Private words )

: str-offset       ( n w:scl - n = Determine offset from index, incl. validation )
  str>length @
  
  over 0< IF                 \ if index < 0 then
    tuck + swap              \   index = index + length
  THEN
  
  over <= over 0< OR IF      \ if index < 0 or index >= length
    exp-index-out-of-range throw 
  THEN
;


: str-data@        ( w:str - a = Get the start of the string )
  str>data @
;


( Public words )

: str-init         ( w:str - = Initialise the empty string )
  dup str>data  nil!
  dup str>length  0!
  dup str>size    0!
  str.extra swap str>extra !
;


: str-create       ( C: "name" - R: - w:str = Create a named string in the dictionary )
  create   here   str% allot   str-init
;


: str-new          ( - w:str = Create a new string on the heap )
  str% allocate  throw  dup str-init
;


: str-free         ( w:str - = Free the string from the heap )
  dup str-data@
  dup nil<> IF               \ If data <> nil Then
    free throw               \   free data
  ELSE
    drop
  THEN
  
  free  throw                \ Free struct
;


: str-empty?       ( w:str - f = Check for empty string )
  str>length @ 0=  
;


: str-length@      ( w:str - u = Get the length of the string )
  str>length @
;


: str-size!        ( u w:str - = Insure the size of the string )
  dup str-data@ nil= IF      \ if data = nil then
    tuck str>extra @ +       \   size = requested + extra
    2dup swap str>size !
    1+ chars allocate throw  \ 
    swap str>data !          \   data = allocated size + 1 for zero terminated string
  ELSE
    2dup str>size @ > IF     \ else if requested > current size then
      tuck str>extra @ +     \   size = requested + extra
      2dup swap str>size !
      1+ chars
      over str-data@ swap    \   reserve extra character for zero terminated string
      resize throw        
      swap str>data !    
    ELSE
      2drop
    THEN
  THEN
;


: str-extra@       ( w:str - u = Get the extra space allocated during resizing of the string )
  str>extra @
;

: str-extra!       ( u w:str - = Set the extra space allocated during resizing of the string )
  str>extra !
;


: str+extra@       ( - u = Get the initial extra space allocated during resizing of the string )
  str.extra
;


: str+extra!       ( u - = Set the initial extra space allocated during resizing of the string )
  to str.extra
;


( String manipulation words )


: str-clear        ( w:str - = Clear the string )
  str>length 0!
;


: str-set          ( c-addr u w:str - = Set a string in the string )
  2dup str-size!             \ check the space
  2dup str>length !          \ set the length
  str-data@ swap chars cmove \ move the string
;


( Private words )

: str-length+!     ( u w:str - u = Increase the length, return the previous length )
  tuck str-length@ +         \ len' = str>length + u
  swap
  2dup str-size!             \ check space
  str>length @!              \ fetch and store the new length
;


: str-insert-space ( u n w:str - u w:data = Insert u chars at nth index )
  tuck str-offset                \ Index -> offset
  >r 2dup str-length+! r>        \ Increase the length
  tuck -                         \ Calculate move length
  >r chars swap str-data@ + r>   \ Calculate start of insert space
  over >r dup 0> IF              \ If move length > 0 then
    >r over chars over + r>      \   Calculate destination of insert space
    cmove>                       \   Move the characters
  ELSE
    2drop
  THEN
  r>                             \ Return the start of the insert space
;


( Public words )

: str-append-string    ( c-addr u w:str - = Append a string to the string )
  2dup str-length+!          \ increase the length
  
  chars swap str-data@ +     \ move the string at the end
  swap cmove
;


: str-prepend-string   ( c-addr u w:str - = Prepend a string to the string )
  2dup str-length+!           \ increase the length
  
  >r 2dup str-data@
  swap chars over + r> cmove> \ move away the current string
  
  str-data@ swap cmove        \ move the new string at the begin
;


: str-append-chars   ( c u w:str - = Append a number of characters )
  2dup str-length+!          \ increase the length
  
  chars swap str-data@ +     \ fill the characters at the end
  -rot swap fill
;


: str-prepend-chars  ( c u w:str - = Prepend a number of characters )
  2dup str-length+!            \ increase the length
  
  >r 2dup str-data@
  swap chars over + r> cmove>  \ move away the current string
  
  str-data@ -rot swap fill     \ fill the characters at the begin
;


: str-insert-string  ( c-addr u n:start w:str - = Insert a string in the string )
  str-insert-space           \ Insert space in the string
  
  swap cmove                 \ Move the string
;


: str-insert-chars ( c u n:start w:str - = Insert a number of characters )
  str-insert-space           \ Insert space in the string
  
  -rot swap fill             \ fill it with the characters
;


: str-get-substring   ( u n:start w:str - w:str2 = Get a substring from start u chars long as a new dyn. string )
  dup >r str-offset          \ Index -> offset
  2dup + r@ str-length@ >    \ If not enough data then exception
    exp-no-data and throw
  chars r> str-data@ +       \ Make the substring
  swap
;


: str-get          ( w:str - c-addr u = Get the string )
  dup  str-data@
  swap str-length@
;


: str-bounds       ( w:str - c-addr+u c-addr = Get the bounds of the string )
  str-get bounds
;  


: str-delete       ( u n w:str - = Delete a substring from nth index and length u from the string )
  dup >r
  str-offset                      \ Index -> offset
  2dup + r@ str-length@ <= IF     \ If offset + length <= length then 
    r@ str-data@ over chars +     \   Calculate destination of move
    rot 2dup chars +              \   Calculate source of move
    swap r@ str-length@ swap -    \   Reduce length
    dup r@ str>length !
    2swap >r - r>                 \   Calculate length of move
    swap dup 0> IF                \   If length of move > 0 then
      cmove                       \     Move the data
    ELSE
      drop 2drop
    THEN
  ELSE
    exp-no-data throw
  THEN
  rdrop
;


: str-set-cstring  ( c-addr w:str - = Set a zero terminated string in the string )
  over 0 swap                \ length = 0
  BEGIN
    dup c@ chr.nul <>        \ while [str] <> 0 do
  WHILE
    swap 1+ swap             \  increase length
    1 chars +                \  increase str
  REPEAT
  drop
  swap str-set               \ set in string
;


: str-get-cstring  ( w:str - c-addr = Get the string as zero terminated string )
  dup str-length@ chars
  swap str-data@
  tuck + chr.nul swap c!     \ store nul at end of string
;


: str^move         ( w:str2 w:str1 - Move str2 in str1 )
  >r str-get r> str-set
;


( Character words )

: str-append-char    ( c w:str - = Append a character at the end of the string )
  1 over str-length+!
  
  chars swap str-data@ + c! \ store char at end of string
;


: str-prepend-char ( c w:str - = Prepend a character at the start of the string )
  >r 1 0 r>
  
  str-insert-space
  nip c!
;


: str-push-char    ( c w:str - = Push a character at the end of the string )
  str-append-char
;


: str-pop-char     ( w:str - c = Pop a character from the end of the string )
  dup str>length dup @ dup 0> IF
    1- tuck swap !
    chars swap str-data@ + c@
  ELSE
    exp-no-data throw
  THEN
;


: str-enqueue-char ( c w:str - = Place a character at the start of the string )
  str-prepend-char  
;


: str-dequeue-char ( c w:str - = Get the character at the end of the string )
  str-pop-char
;


: str-set-char     ( c n w:str - = Set the character on the nth position in the string )
  tuck str-offset 
  chars swap str-data@ + c!
;


: str-get-char     ( n w:str - c = Get the character from the nth position in the string )
  tuck str-offset
  chars swap str-data@ + c@
;


: str-insert-char  ( c n w:str - = Insert the character on the nth position in the string )
  2>r 1 2r> 
  str-insert-space
  nip c!
;


: str-delete-char  ( n w:str - = Delete the character on the nth position in the string )
  2>r 1 2r>
  str-delete
;


: str-execute      ( ... xt w:str - ... = Execute the xt token for every character in the string )
  str-bounds ?DO             \ Do for string
    I c@
    swap dup >r
    execute                  \  Execute token for character with stack cleared
    r>
    1 chars +LOOP
  drop
;


( Special manipulation words )

: str-capatilize   ( w:str - = Capatilize the first word in the string )
  str-bounds ?DO             \ Do for the string
    I c@
    chr-alpha? IF            \   If alpha character then
      I c@ chr-upper I c!    \     Convert to upper
      LEAVE                  \     Done
    THEN
    1 chars 
  +LOOP
;

  
: str-cap-words    ( w:str - = Capatilize all words in the string )
  false swap str-bounds ?DO  \ Do for the string
    I c@ 
    chr-alpha? tuck IF       \   If alpha character then
      0= IF                  \     If previous was not then
        I c@ chr-upper I c!  \       Convert to upper
      THEN
    ELSE
      drop
    THEN
    1 chars
  +LOOP
  drop
;


: str-center       ( u w:str - = Center the string in u width )
  dup >r str-length@ - dup 0> IF
    dup 2/ swap over -
    chr.sp swap r@ str-append-chars  
    chr.sp swap r@ str-prepend-chars
  ELSE
    drop
  THEN
  rdrop
;


: str-align-left   ( u w:str - = Align left the string in u width )
  tuck str-length@ - dup 0> IF
    chr.sp -rot swap str-append-chars
  ELSE
    2drop
  THEN
;


: str-align-right  ( u w:str - = Align right the string in u width )
  tuck str-length@ - dup 0> IF
    chr.sp -rot swap str-prepend-chars
  ELSE
    2drop
  THEN
;


: str-zfill        ( u w:str - = Right justify the string with leading zero's )
  tuck str-length@ - dup 0> IF
    [char] 0 -rot swap str-prepend-chars
  ELSE
    2drop
  THEN
;


: str-strip-leading  ( w:str - = Strip leading spaces in the string )
  0 over str-bounds ?DO
    I c@ chr-blank? IF       \ Count the number of leading spaces
      1+
    ELSE
      LEAVE
    THEN
    1 chars 
  +LOOP
  
  ?dup IF                   
    swap 0 swap str-delete   \ Delete the spaces
  ELSE
    drop
  THEN
;


: str-strip-trailing  ( w:str - = Strip trailing spaces in the string )
  0 over str-bounds swap
  BEGIN
    1 chars -
    2dup <= dup IF
      drop dup c@ chr-blank?   \ Count the number of trailing spaces
    THEN
  WHILE
    rot 1+ -rot
  REPEAT
  
  2drop ?dup IF
    negate swap str>length +!  \ Delete the spaces by reducing the length
  ELSE
    drop
  THEN
;


: str-strip        ( w:str - = Strip leading and trailing spaces in the string )
  dup str-strip-leading
      str-strip-trailing
;


: str-lower        ( w:str - = Convert the string to lower case )
  str-bounds ?DO
    I c@ chr-lower I c!
    1 chars
  +LOOP
;


: str-upper        ( w:str - = Convert the string to upper case )
  str-bounds ?DO
    I c@ chr-upper I c!
    1 chars
  +LOOP
;


: str-expand-tabs  ( u w:str - = Expand the tabs to u spaces in the string )
  >r 0                                 \ Offset = 0
  BEGIN
    dup r@ str-length@ <               \ While offset < length do
  WHILE
    dup r@ str-get-char chr.ht = IF    \   If str[offset] = tab then
      over 1 = IF                      \     If replace by one space then
        chr.sp over r@ str-set-char    \       Set the space
        1 chars +
      ELSE                             \     Else
        over 0> IF                     \       If replace by more spaces then
          2dup r@ str-insert-space     \         Insert space and fill with blanks
          swap blank
          over chars +
        THEN
        1 over r@ str-delete           \       Delete the tab
      THEN
    ELSE
      1 chars +
    THEN
  REPEAT
  2drop
  rdrop
;


( Comparison words )

: icompare         ( c-addr u c-addr u - n = Compare case-insensitive two strings )
  rot swap 2swap 2over
  min 0 ?DO
    over c@ chr-upper over c@ chr-upper - sgn ?dup IF
      >r 2drop 2drop r>
      unloop 
      exit
    THEN
    1 chars + swap 1 chars + swap
  LOOP
  2drop
  - sgn
;
  
  
: str-icompare     ( c-addr u w:str - n = Compare case-insensitive a string with the string )
  str-get icompare
;


: str-ccompare     ( c-addr u w:str - n = Compare case-sensitive a string with the string )
  str-get compare
;


: str^icompare     ( w:str w:str - n = Compare case-insensitive two strings )
  >r str-get r> str-icompare
;


: str^ccompare     ( w:str w:str - n = Compare case-sensitive two strings )
  >r str-get r> str-ccompare
;


: str-count        ( c-addr u w:str - u = Count the number of occurences of a string in the string )
  >r 0 -rot 
  r> str-get
  rot
  tuck - 1+                  \ Determine the maximum length for full string
  dup 0<= IF
    2drop 2drop
  ELSE
    >r swap r> bounds DO
      2dup I over compare 0= IF  \ Test all substrings for success ..
        rot 1+ -rot              \ .. and count them
      THEN
      1 chars
    +LOOP
    2drop
  THEN
;


: str-find         ( c-addr u n w:str - n = Find the first occurence of a string from nth position in the string )
  dup >r str-offset                    \ Index -> offset
  over r@ str-length@ swap -          
  over - 1+                            \ Determine the remaing length
  over r> str-data@ swap chars +       \ Determine the start of data with the offset
  -rot dup 0<= IF                      \ Check for sufficient remaining length
    2drop
  ELSE
    over + swap DO                     \ Search the string from offset till end
      over 2over 2over compare 0= IF   \ If found then
        2drop 2drop
        I UNLOOP EXIT                  \  Return the index
      THEN
      drop
      1 chars +
    LOOP
  THEN
  2drop drop
  -1
;

  
: str-replace      ( c-addr u c-addr u w:str - = Replace the occurences of the second string with the first string in the string )
  >r 0
  BEGIN
    dup r@ str-length@ < dup IF   \ If index is lower than the length
      drop
      >r 2dup r>
      r@ str-find                 \   Find the search string in the string
      dup -1 <>
    THEN
  WHILE                           \ While found do
    >r 2swap 2dup r>
    r@ over
    >r str-insert-string dup r> + \   Insert the new string in the string
    >r 2swap dup r>
    r@ over
    >r str-delete r>              \   Delete the search string
  REPEAT
  drop 2drop 2drop
  rdrop
;


( Inspection )

: str-dump         ( w:str - = Dump the string )
  ." str:" dup . cr
  ."  data  :" dup str>data ? cr
  ."  length:" dup str>length ? cr
  ."  size  :" dup str>size ? cr
  ."  extra :" str>extra ? cr
;

[THEN]

\ ==============================================================================
