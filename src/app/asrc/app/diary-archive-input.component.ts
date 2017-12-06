import { Component, OnInit } from '@angular/core';
import { ComponentBase } from './component-base';
import { slideInDownAnimation } from './animations';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';
import { ArchiveInputDataService } from '../services/archive-input-service';
import { DiaryArchiverInputData } from '../common/diary-archiver-input-data';
import { FormControl } from '@angular/forms';

declare var electron: Electron.AllElectron;

const currentWindow = electron.remote.getCurrentWindow();
const dialog = electron.remote.dialog;

@Component({
  selector: 'app-diary-archive-input',
  templateUrl: './diary-archive-input.component.html',
  styleUrls: ['./diary-parse-input.component.css'],
  animations: [slideInDownAnimation]
})

export class DiaryArchiveInputComponent extends ComponentBase implements OnInit {
  title: string = 'Архивирование данных';
  menuEnabled: boolean = true;

  constructor(router: Router,
    appStateService: AppStateService,
    protected archiveInputDataService: ArchiveInputDataService) {
    super(router, appStateService)

  }

  archiveInputData: DiaryArchiverInputData = new DiaryArchiverInputData();

  ngOnInit() {
    var sub = this.archiveInputDataService.currentData.subscribe(archiveInputData => this.archiveInputData = archiveInputData);
    this.subscriptions.push(sub);
    super.ngOnInit();
  }

  

  onDiaryDirectoryButtonClick(workingDirFormControl: FormControl) {
    let paths = dialog.showOpenDialog(currentWindow, {
      properties: ['openDirectory']
    });

    if (paths && paths.length > 0) {
      this.archiveInputData.diaryDir = paths[0];
    }
  }

  onSubmit() {
    this.archiveInputDataService.changeData(this.archiveInputData);
    this.router.navigateByUrl("/archiveprogress");
  }

}
