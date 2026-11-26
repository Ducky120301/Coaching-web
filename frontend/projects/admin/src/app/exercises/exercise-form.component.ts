import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { formatRange, parseRange, parseWhole } from './dose-format';
import { Categories, CategoryKind, ExerciseDetail, SaveExerciseBody } from './exercise.models';
import { ExercisesApi, problemCode, problemTitle } from './exercises.api';

/** Câu 409 phiên bản nguyên văn tech-design (@BR-f03-thu-vien-bai-tap-020). */
export const VERSION_CONFLICT = 'Bài vừa được sửa ở nơi khác. Tải lại để xem bản mới nhất.';
export const FIX_FIELDS = 'Có ô chưa đúng. Sửa các ô được đánh dấu đỏ.';

type FieldKey = 'name' | 'muscleGroupId' | 'equipmentId' | 'videoUrl' | 'sets' | 'reps' | 'rir' | 'rest' | 'techniqueNotes' | 'commonMistakes';
type Values = Record<FieldKey, string>;
type Errors = Partial<Record<FieldKey, string>>;

const EMPTY: Values = {
  name: '', muscleGroupId: '', equipmentId: '', videoUrl: '', sets: '', reps: '', rir: '', rest: '', techniqueNotes: '', commonMistakes: '',
};

/** Lỗi 400 của API → ô hiển thị `title` (BR-002…004, 007, 009…014). */
const FIELD_BY_CODE: Record<string, FieldKey> = {
  'exercise.name_required': 'name',
  'exercise.name_too_long': 'name',
  'exercise.muscle_group_required': 'muscleGroupId',
  'exercise.equipment_required': 'equipmentId',
  'exercise.video_not_youtube': 'videoUrl',
  'exercise.sets_out_of_range': 'sets',
  'exercise.reps_invalid': 'reps',
  'exercise.rir_invalid': 'rir',
  'exercise.rest_out_of_range': 'rest',
};

/**
 * Thêm bài (thu-vien/bai-tap/moi) / Sửa bài (thu-vien/bai-tap/:id/sua) — kiểu form AdTaoHoSo (prototype chưa có màn này).
 * @ADR-f03-thu-vien-bai-tap-006: liều nhập chữ "10–12", gửi API min/max.
 */
@Component({
  selector: 'app-exercise-form',
  imports: [RouterLink],
  templateUrl: './exercise-form.component.html',
  styleUrl: './exercise-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExerciseFormComponent implements OnInit {
  /** Route data: 'create' | 'edit'. */
  readonly mode = input<'create' | 'edit'>('create');
  /** Route param `:id` (chế độ sửa). */
  readonly id = input<string>();

  private readonly api = inject(ExercisesApi);
  private readonly router = inject(Router);

  protected readonly values = signal<Values>({ ...EMPTY });
  protected readonly errors = signal<Errors>({});
  protected readonly categories = signal<Categories | null>(null);
  protected readonly loaded = signal<ExerciseDetail | null>(null);
  protected readonly loadFailed = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly conflict = signal(false);
  protected readonly hiddenDuplicateId = signal<string | null>(null);
  protected readonly adding = signal<CategoryKind | null>(null);
  protected readonly newCategory = signal('');
  protected readonly categoryError = signal<string | null>(null);
  protected readonly versionConflict = VERSION_CONFLICT;

  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly title = computed(() => (this.isEdit() ? 'Sửa bài' : 'Thêm bài mới'));
  protected readonly cancelQuery = computed(() => (this.isEdit() && this.id() ? { bai: this.id() } : {}));

  async ngOnInit(): Promise<void> {
    try {
      const [cats, current] = await Promise.all([
        firstValueFrom(this.api.categories()),
        this.isEdit() ? firstValueFrom(this.api.get(this.id()!)) : Promise.resolve(null),
      ]);
      this.categories.set(cats);
      if (current) this.fill(current);
    } catch {
      this.loadFailed.set(true);
    }
  }

  protected setValue(key: FieldKey, value: string): void {
    this.values.set({ ...this.values(), [key]: value });
    if (this.errors()[key]) {
      const rest = { ...this.errors() };
      delete rest[key];
      this.errors.set(rest);
    }
  }

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.saving() || (this.isEdit() && !this.loaded())) return;
    const body = this.toBody();
    if (!body) return;
    this.saving.set(true);
    this.message.set(null);
    this.conflict.set(false);
    this.hiddenDuplicateId.set(null);
    try {
      const current = this.loaded();
      const saved = await firstValueFrom(current
        ? this.api.update(current.id, { ...body, version: current.version })
        : this.api.create(body));
      await this.router.navigateByUrl(`/thu-vien/bai-tap?bai=${saved.id}`);
    } catch (error) {
      this.showError(error);
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-001: trùng bài đang ẩn → bỏ ẩn bài đó rồi mở nó trong thư viện
  protected async unhideDuplicate(): Promise<void> {
    const id = this.hiddenDuplicateId();
    if (!id || this.saving()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.unhide(id));
      await this.router.navigateByUrl(`/thu-vien/bai-tap?bai=${id}`);
    } catch (error) {
      this.hiddenDuplicateId.set(null);
      this.message.set(problemTitle(error, 'Không bỏ ẩn được bài. Vui lòng thử lại.'));
    } finally {
      this.saving.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-020: tải bản mới nhất (bỏ phần đang sửa)
  protected async reload(): Promise<void> {
    try {
      this.fill(await firstValueFrom(this.api.get(this.id()!)));
      this.conflict.set(false);
      this.message.set(null);
    } catch {
      this.message.set('Không tải được bài. Vui lòng thử lại.');
    }
  }

  protected openAdd(kind: CategoryKind): void {
    this.adding.set(kind);
    this.newCategory.set('');
    this.categoryError.set(null);
  }

  protected cancelAdd(): void {
    this.adding.set(null);
    this.categoryError.set(null);
  }

  // @BR-f03-thu-vien-bai-tap-006: thêm mục mới tại chỗ, chọn luôn cho bài đang soạn
  protected async addCategory(): Promise<void> {
    const kind = this.adding();
    const name = this.newCategory().trim();
    if (!kind) return;
    if (!name) {
      this.categoryError.set('Nhập tên mục.');
      return;
    }
    this.categoryError.set(null);
    try {
      const created = await firstValueFrom(this.api.addCategory(kind, name));
      const cats = this.categories() ?? { muscleGroups: [], equipment: [] };
      const item = { id: created.id, name: created.name };
      this.categories.set(kind === 'MuscleGroup'
        ? { ...cats, muscleGroups: [...cats.muscleGroups, item] }
        : { ...cats, equipment: [...cats.equipment, item] });
      this.setValue(kind === 'MuscleGroup' ? 'muscleGroupId' : 'equipmentId', created.id);
      this.adding.set(null);
    } catch (error) {
      this.categoryError.set(problemTitle(error, 'Không thêm được mục. Vui lòng thử lại.'));
    }
  }

  private fill(d: ExerciseDetail): void {
    this.loaded.set(d);
    this.values.set({
      name: d.name,
      muscleGroupId: d.muscleGroupId,
      equipmentId: d.equipmentId,
      videoUrl: d.videoUrl ?? '',
      sets: d.sets === null ? '' : `${d.sets}`,
      reps: formatRange(d.repsMin, d.repsMax, ''),
      rir: formatRange(d.rirMin, d.rirMax, ''),
      rest: d.restSeconds === null ? '' : `${d.restSeconds}`,
      techniqueNotes: d.techniqueNotes ?? '',
      commonMistakes: d.commonMistakes ?? '',
    });
    this.errors.set({});
  }

  /** FE chỉ kiểm bắt buộc + định dạng; khoảng hợp lệ do Domain kiểm (BR-009…014). */
  private toBody(): SaveExerciseBody | null {
    const v = this.values();
    const errors: Errors = {};
    const name = v.name.trim();
    if (!name) errors.name = 'Nhập tên bài.';
    if (!v.muscleGroupId) errors.muscleGroupId = 'Chọn nhóm cơ.';
    if (!v.equipmentId) errors.equipmentId = 'Chọn dụng cụ.';
    const sets = parseWhole(v.sets);
    if (sets === 'invalid') errors.sets = 'Nhập một số nguyên, VD: 3.';
    const reps = parseRange(v.reps);
    if (reps === 'invalid') errors.reps = 'Nhập một số hoặc khoảng, VD: 10–12.';
    const rir = parseRange(v.rir);
    if (rir === 'invalid') errors.rir = 'Nhập một số hoặc khoảng, VD: 1–2.';
    const rest = parseWhole(v.rest);
    if (rest === 'invalid') errors.rest = 'Nhập số giây, VD: 90.';
    this.errors.set(errors);
    if (sets === 'invalid' || reps === 'invalid' || rir === 'invalid' || rest === 'invalid' || Object.keys(errors).length > 0) {
      this.message.set(FIX_FIELDS);
      this.hiddenDuplicateId.set(null);
      this.conflict.set(false);
      return null;
    }
    return {
      name,
      muscleGroupId: v.muscleGroupId,
      equipmentId: v.equipmentId,
      videoUrl: v.videoUrl.trim() || null,
      sets,
      repsMin: reps.min,
      repsMax: reps.max,
      rirMin: rir.min,
      rirMax: rir.max,
      restSeconds: rest,
      techniqueNotes: v.techniqueNotes.trim() || null,
      commonMistakes: v.commonMistakes.trim() || null,
    };
  }

  private showError(error: unknown): void {
    const code = problemCode(error);
    if (code === 'exercise.version_conflict') {
      this.conflict.set(true);
      return;
    }
    const title = problemTitle(error, 'Không lưu được bài. Vui lòng thử lại.');
    if (code === 'exercise.duplicate') {
      const hidden = (error as { error?: { hiddenExerciseId?: unknown } }).error?.hiddenExerciseId;
      this.hiddenDuplicateId.set(typeof hidden === 'string' ? hidden : null);
      this.message.set(typeof hidden === 'string' ? `${title} Bài đó đang ẩn.` : title);
      return;
    }
    const field = code ? FIELD_BY_CODE[code] : undefined;
    if (field) {
      this.errors.set({ ...this.errors(), [field]: title });
      this.message.set(FIX_FIELDS);
      return;
    }
    this.message.set(title);
  }
}
