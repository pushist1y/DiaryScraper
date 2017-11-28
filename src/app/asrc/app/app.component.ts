import { Component } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { AppStateService } from '../services/appstate.service';
import { ApplicationState } from '../common/app-state';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  animations: [slideInDownAnimation]
})
export class AppComponent {

  constructor(private appStateService: AppStateService) {

  }

  private appState: ApplicationState = new ApplicationState();

  ngOnInit() {
    this.appStateService.currentState.subscribe(appState => setTimeout(() => {
      this.appState.menuEnabled = appState.menuEnabled;
      this.appState.title = appState.title;
    }, 0));
    let appState = new ApplicationState();
    appState.title = 'Выгрузка дневников';
    appState.menuEnabled = true;
    this.appStateService.changeState(appState);
  }
}
