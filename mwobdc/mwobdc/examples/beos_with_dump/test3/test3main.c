#include <stdio.h>

extern void gint_inc(int v);
extern void gint_dec(int v);
extern int gint_peek();

int main()
{
  int v1 = 0, v2 = 0;

  gint_inc(10);
  v1 = gint_peek();
  gint_dec(5);
  v2 = gint_peek();

  printf("%i %i\n", v1, v2);

  return 0;
}
