char test1(char value)
{
  return value;
}

int test2(int value)
{
  return value;
}

int test3(int value1, int value2)
{
  return value1 + value2;
}

static int static_value = 0; 

void test4_1()
{
  static_value++;
}

int test4_2()
{
  return static_value;
}

void test4_3(int increment)
{
  static_value = test3(static_value, increment);
}
