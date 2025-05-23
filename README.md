# Лабораторная работа 1  
**Тема:** Разработка пользовательского интерфейса (GUI) для языкового процессора  
- **Цель:** Разработать приложение – текстовый редактор.
---


## Основные функции  

### Файл  
- **Создать**  
  Создает новый файл или проект.  
- **Открыть**  
  Открывает существующий файл или проект из файловой системы.  
- **Сохранить**  
  Сохраняет текущий файл.  
- **Сохранить как**  
  Сохраняет текущий файл с новым именем или в новом месте.  
- **Выход**  
  Закрывает IDE.  

### Правка  
- **Отменить**  
  Отменяет последнее действие.  
- **Повторить**  
  Повторяет отмененное действие.  
- **Вырезать**  
  Удаляет выделенный текст или элемент и помещает его в буфер обмена.  
- **Копировать**  
  Копирует выделенный текст или элемент в буфер обмена.  
- **Вставить**  
  Вставляет содержимое буфера обмена в текущее место курсора.  
- **Удалить**  
  Удаляет выделенный текст или элемент без помещения в буфер обмена.  
- **Выделить все**  
  Выделяет весь текст или элемент в текущем окне или документе.  

## Дополнительныйе задания
- **Изменение размеров текста в окне редактирования и окне вывода результатов.**
- **Интерфейс с вкладками, позволяющий одновременно работать с несколькими текстами (для окна редактирования).**
- **Выбор языка интерфейса приложения (интернационализация).**
- **Нумерация строк в окне редактирования текста.**
- **Открытие файла при перетаскивании иконки в окно программы.**
- **Наличие строки состояния для отображения текущей информации о состоянии работы приложения.**
- **Базовая подсветка синтаксиса в окне редактирования.**
- **Интерфейс с вкладками, позволяющий работать с разными модулями программы (для окна вывода результатов)**
- **Отображение ошибок в окне вывода результатов в виде таблицы.**
- **Горячие клавиши для быстрых команд.**
---

# Лабораторная работа 2  
**Тема:** Разработка лексического анализатора (сканера)
 **Вариант 36. Объявление вещественной константы с инициализацией в СУБД PostgreSQL**
 **Вводные данные:**
 - DECLARE VAT CONSTANT NUMERIC := 0.1;
   
---
 **Цель работы:** Изучить назначение лексического анализатора. Спроектировать алгоритм и выполнить программную реализацию сканера.
- В соответствии с вариантом задания необходимо:
- Спроектировать диаграмму состояний сканера (примеры диаграмм представлены в прикрепленных файлах).
- Разработать лексический анализатор, позволяющий выделить в тексте лексемы, иные символы считать недопустимыми (выводить ошибку).
- Встроить сканер в ранее разработанный интерфейс текстового редактора. Учесть, что текст для разбора может состоять из множества строк.
---
![image](https://github.com/user-attachments/assets/a6e7435c-72e3-4f5c-8b89-c430005d06f1)
рис. 1 диаграмма сканера
![image](https://github.com/user-attachments/assets/9d27773f-2ea4-4e15-a6d1-d9a7651b203a)
рис. 2 работа сканера

# Лабораторная работа 3
**Тема:** Разработка синтаксического анализатора (парсера)
**Цель работы:** Изучить назначение синтаксического анализатора. Спроектировать алгоритм и выполнить программную реализацию парсера.
 **Вариант 36. Объявление вещественной константы с инициализацией в СУБД PostgreSQL**
 **Вводные данные:**
 - DECLARE VAT CONSTANT NUMERIC := 0.1;
---
### Требования к программе:
-    Результатом анализа правильной строки является вывод сообщения об отсутствии ошибок.
-    Если анализируемая строка содержит ошибки, то выводятся сообщения о них, неверный фрагмент (символ) и его местоположение.
-    В окне вывода результатов выводится количество ошибок.
### **Грамматика:**
1.	‹Start› → ‘DECLARE’‹SP1›
2.	‹SP1› → ‘ ’‹LT›
3.	‹LT› → ‹ letter›‹LT›
4.	‹LT› → ‹ letter›‹DG›
5.	‹LT› → ‹ letter›‹DG›
6.	‹LT› → ‹ letter›‹SP2›
7.	‹DG› → ‹digit›‹DG›
8.	‹DG› → ‹digit›‹LT›
9.	‹DG› → ‹digit›‹SP2›
10.	‹SP2› → ‘ ’‹CNT›
11.	‹CNT› → ‘CONSTANT’‹SP3›
12.	‹SP3› → ‘ ’‹NUM›
13.	‹NUM › → ‘NUMERIC’‹SP4›
14.	‹SP4› → ‘ ’‹AST›
15.	‹AST› → ‘:=’‹SP5›
16.	‹SP5› → ‘ ’‹DG1›
17.	‹DG1› → ‹digit›‹DG1›
18.	‹DG1› → ‘.’‹DG2›
19.	‹DG2› → ‹digit›‹DG2›
20.	‹DG2› → ‹digit›‹END›
21.	‹End› → ‘;’
- ‹digit› → “0” | “1” | “2” | “3” | “4” | “5” | “6” | “7” | “8” | “9”
- ‹letter› → “a” | “b” | “c” | ... | “z” | “A” | “B” | “C” | ... | “Z”

![image](https://github.com/user-attachments/assets/87c36315-1ad7-4b6c-8322-cf4f38f7fa0c)
рис. 3 работа парсера

---

# Лабораторная работа 4
**Цель работы:** Реализовать алгоритм нейтрализации синтаксических ошибок и дополнить им программную реализацию парсера.
**Задание:** Реализовать алгоритм синтаксического анализа с нейтрализацией ошибок (метод Айронса). 

---

# Лабораторная работа 5
**Тема:** Включение семантики в анализатор. Создание внутренней формы представления программы. Цель работы: Дополнить анализатор, разработанный в рамках лабораторных работ, этапом формирования внутренней формы представления программы.

**1 вариант.** В качестве внутренней формы представления программы выберем польскую инверсную запись (ПОЛИЗ). Эта форма представления наглядна и достаточно проста для последующей интерпретации, которая может быть выполнена с использованием стека.

**Задание:**

Реализовать в текстовом редакторе поиск лексических и синтаксических ошибок для грамматики G[]. Реализовать данную КС-граммматику методом рекурсивного спуска:
E → TA
A → ε | + TA | - TA
T → ОВ
В → ε | *ОВ | /ОВ
О → num | (E)
num → digit {digit}
Реализовать алгоритм записи арифметических выражений в ПОЛИЗ и алгоритм вычисления выражений в ПОЛИЗ.
изображение

![image](https://github.com/user-attachments/assets/392d6710-e39f-44b9-b50b-e8a7657f2296)

рис.7 Пример с ошибкой

![image](https://github.com/user-attachments/assets/755ff160-6b7f-457c-b4a6-a3fac1015b7e)

рис.8 Верно решенный пример

---

# Лабораторная работа 6

**Тема:** Реализация алгоритма поиска подстрок с помощью регулярных выражений.

**Цель:** Реализовать алгоритм поиска в тексте подстрок, соответствующих заданным регулярным выражениям Задания:

(21). Построить РВ, описывающее целые числа и числа с плавающей точкой (разделитель запятая).
РВ:"-?\d+(?:,\d+)?"

(1). Построить РВ, описывающее стандартный формат юзернейма (содержит цифры, строчные буквы, символы - и _, имеет длину от 5 до 20 знаков).
РВ:"\b[a-z0-9_-]{5,20}\b"

(17). Построить РВ, описывающее широту (учесть диапазон корректных значений).
РВ:"^[-+]?(?:90(?:\.0+)?|(?:[1-8]?\d(?:\.\d+)?))"


![image](https://github.com/user-attachments/assets/dd26b58f-07c9-4172-855f-dfbee287d496)

рис.9 Пример нахождения числа

![image](https://github.com/user-attachments/assets/667bc443-f84e-48b2-8374-873cf3f23765)

рис.10 Пример нахождения юзернейма

![image](https://github.com/user-attachments/assets/c339f56e-754a-4d00-90ae-11683dcafd5d)

рис.11 Пример нахождения широты

---

**Доп.Задание**
![image](https://github.com/user-attachments/assets/57fd318a-549f-41e0-84f2-9502fafcbb71)

рис.12 Граф автомата
