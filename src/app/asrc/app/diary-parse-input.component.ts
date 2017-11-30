import { Component, OnInit, HostBinding } from '@angular/core';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';
import { slideInDownAnimation } from './animations';
import { Subscription } from 'rxjs';
import { ApplicationState } from '../common/app-state';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { ParseInputDataService } from '../services/parse-input-service';
import { FormControl } from '@angular/forms';

declare var electron: Electron.AllElectron;

const currentWindow = electron.remote.getCurrentWindow();
const dialog = electron.remote.dialog;

@Component({
  selector: 'diary-parse',
  templateUrl: './diary-parse-input.component.html',
  styleUrls: ['./diary-parse-input.component.css'],
  animations: [slideInDownAnimation]
})
export class DiaryParseInputComponent implements OnInit {

  constructor(private router: Router,
    private appStateService: AppStateService,
    private parseInputService: ParseInputDataService) {

  }

  @HostBinding('@routeAnimation') routeAnimation = true;
  @HostBinding('style.display') display = 'block';

  private subscriptions: Array<Subscription> = new Array<Subscription>();
  private appState: ApplicationState = new ApplicationState();
  private parseInputData: DiaryParserInputData = new DiaryParserInputData();

  ngOnInit() {
    var sub = this.parseInputService.currentData.subscribe(newParseData => this.parseInputData = newParseData);
    this.subscriptions.push(sub);

    sub = this.appStateService.currentState.subscribe(newState => this.appState = newState);
    this.subscriptions.push(sub);

    this.appState.menuEnabled = true;
    this.appState.title = 'Обработка данных';
    this.appStateService.changeState(this.appState);
  }

  ngOnDestroy() {
    if (!this.subscriptions) {
      return;
    }
    this.subscriptions.forEach((sub) => {
      sub.unsubscribe();
    })
  }

  onDiaryDirectoryButtonClick(workingDirFormControl: FormControl) {
    let paths = dialog.showOpenDialog(currentWindow, {
      properties: ['openDirectory']
    });

    if (paths && paths.length > 0) {
      this.parseInputData.diaryDir = paths[0];
    }
  }

  onSubmit() {
    this.parseInputService.changeData(this.parseInputData);
    this.router.navigateByUrl("/parseprogress");
  }

}
