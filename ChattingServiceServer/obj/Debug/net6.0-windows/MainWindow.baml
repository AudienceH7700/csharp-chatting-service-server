   M S B A M L   `   `   `  ����  ChattingServiceServer&     ChattingServiceServer.MainWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Chatting Server��5   	   $	��550��6!   $
��1000��6.   .��5      �  � ColumnDefinitions 5   
   �� 5      $��50 �6   6,   �� 5      $��1* �6   6,   �� 5	      $��2* �6   6,   .+�5   
   � $��0��6   .+�5      �� .+�5      �� $��1,1,1,0q�6   
���   �._�5      �� $
��User��6     �� TextAlignment$ Center=�6,   $��20��6C   5      �� 5      $��1,1,1,1q�6   
���   �$	��500��6G   ._�5      �� -   5         ��Name$ UserListView��5      $��0q�65   5      5      5      � 5   
   $��1��6   .+�5      �� .+�5      �� $��0,1,1,0q�6   
���   �._�5      �� $��	AccessLog��6    $ Center=�61   $��20��6H   5      �� 5      $��0,1,1,1q�6   
���   �$	��500��6G   ._�5      �� -   5        $ AccessLogListView��5      $��0q�6:   5       5!      5$      � 5&   
   $��2��6   .+�5'      �� .+�5(      �� $��0,1,1,0q�6   
���   �._�5)      �� $��MainLog��6    $ Center=�6/   $��20��6F   5*      �� 5,      $��0,1,1,1q�6   
���   �$	��500��6G   ._�5-      �� -   5        $ ChattingLogListView��5-      $��0q�6<   5.      5/      52      53      54      