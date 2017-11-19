import { Component } from '@angular/core';
import { slideInDownAnimation } from './animations';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  animations: [slideInDownAnimation]
})
export class AppComponent {
  title = 'Выгрузка дневников';
}
