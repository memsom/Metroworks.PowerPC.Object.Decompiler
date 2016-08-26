static int gint = 0x0A;

void gint_inc(int increment)
{
  gint += increment;
}

void gint_dec(int decrement)
{
  gint -= decrement;
}

int  gint_peek()
{
  return gint;
}


