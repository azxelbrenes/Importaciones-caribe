/// Espejo de Pagina<T> del backend.
///
/// TotalPaginas y las banderas vienen calculadas del servidor: si
/// cada pantalla hiciera esa aritmetica por su cuenta, tarde o
/// temprano una la haria mal.
export interface Pagina<T> {
  items: T[];
  numeroPagina: number;
  porPagina: number;
  totalRegistros: number;
  totalPaginas: number;
  hayAnterior: boolean;
  haySiguiente: boolean;
}
