grammar LabCalculator;

compileUnit : expression EOF;

expression :
    LPAREN expression RPAREN                                              #ParenthesizedExpr
  | operatorToken=(ADD | SUBTRACT) expression                             #UnaryExpr
  | expression operatorToken=(MULTIPLY | DIVIDE | MOD | DIV) expression   #MultiplicativeExpr
  | expression operatorToken=(ADD | SUBTRACT) expression                  #AdditiveExpr
  | funcName=(MAX | MIN) LPAREN expression COMMA expression RPAREN        #FunctionExpr
  | NUMBER                                                                #NumberExpr
  | IDENTIFIER                                                            #IdentifierExpr
  ;

NUMBER     : [0-9]+;
IDENTIFIER : [a-zA-Z]+[0-9]+;

MULTIPLY : '*';
DIVIDE   : '/';
SUBTRACT : '-';
ADD      : '+';
LPAREN   : '(';
RPAREN   : ')';
COMMA    : ',';

MOD : [mM][oO][dD];
DIV : [dD][iI][vV];
MAX : [mM][aA][xX];
MIN : [mM][iI][nN];

WS : [ \t\r\n] -> channel(HIDDEN);
