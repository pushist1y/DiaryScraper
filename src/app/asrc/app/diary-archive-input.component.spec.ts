import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DiaryArchiveInputComponent } from './diary-archive-input.component';

describe('DiaryArchiveInputComponent', () => {
  let component: DiaryArchiveInputComponent;
  let fixture: ComponentFixture<DiaryArchiveInputComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DiaryArchiveInputComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DiaryArchiveInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
