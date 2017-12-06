import { Component, OnInit, HostBinding } from '@angular/core';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';
import { slideInDownAnimation } from './animations';
import { Subscription } from 'rxjs';
import { ApplicationState } from '../common/app-state';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { ParseInputDataService } from '../services/parse-input-service';
import { FormControl } from '@angular/forms';
import { ComponentBase } from './component-base';

declare var electron: Electron.AllElectron;

const currentWindow = electron.remote.getCurrentWindow();
const dialog = electron.remote.dialog;

@Component({
  selector: 'diary-parse',
  templateUrl: './diary-parse-input.component.html',
  styleUrls: ['./diary-parse-input.component.css'],
  animations: [slideInDownAnimation]
})
export class DiaryParseInputComponent extends ComponentBase implements OnInit {
  title: string = 'Обработка данных';
  menuEnabled: boolean = true;

  constructor(router: Router,
    appStateService: AppStateService,
    protected parseInputService: ParseInputDataService) {
    super(router, appStateService)

  }

  parseInputData: DiaryParserInputData = new DiaryParserInputData();

  ngOnInit() {
    var sub = this.parseInputService.currentData.subscribe(newParseData => this.parseInputData = newParseData);
    this.subscriptions.push(sub);
    super.ngOnInit();
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
